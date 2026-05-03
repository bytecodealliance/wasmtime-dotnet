# Component Model — pending follow-ups from branch review

A `/branch-review` pass on this branch surfaced eleven items. Five were addressed
in the work that already landed on `component-model` (notably the
`option<option<T>>` → `Option<T>` fix in `652307a`). The rest are tracked here
because they need either a wasmtime upgrade, a deeper refactor than fits this
branch, or just dedicated test coverage. None of them is fully closed — every
item below must be paired with a regression test before merge.

## Blocking

### 1. `ComponentValue.ownsHeap` squats on Rust's enum padding

`src/Components/ComponentValue.cs` carries a managed-only `ownsHeap` byte at
offset 1 of a struct that mirrors `wasmtime_component_val_t`. The Rust side is
`#[repr(C, u8)]`, which leaves bytes 1–7 as alignment padding and explicitly
does not guarantee they're zero. Today the test suite happens to land zeroes
there, so `Free()` short-circuits on wasmtime-filled results and the program
limps along — but on any future allocator pattern the byte can be non-zero,
the `Free` switch will fire on Rust-allocated pointers, and the process will
crash with a heap corruption.

The fix needs ownership to live outside the C ABI footprint. Two viable
shapes:

- A managed-only sidecar (`ConditionalWeakTable<ComponentValue, ...>` keyed by
  pointer, or a `Dictionary<UIntPtr, OwnerInfo>`) that the factories populate
  and `FreeManaged` consults.
- A scope wrapper (`ComponentValueScope : IDisposable`) that owns the array of
  managed-side allocations and disposes them en masse; the raw `ComponentValue`
  array stays internal.

Either way `Free()` splits into:

- `FreeManaged()` — for values built by `From*` factories. Releases via
  `Marshal.FreeHGlobal`.
- `ReleaseRustOwned(ref ComponentValue)` — for values that wasmtime wrote.
  Wraps `wasmtime_component_val_delete` (`drop_in_place`) so Rust frees its
  own `Vec`/`String`/`Box`.

### 2. Composite return values from exports leak Rust-allocated memory

Every export that returns `string`, `list`, `record`, `tuple`, `variant`,
`flags`, `option<composite>`, or `result<composite, ...>` currently leaks the
`Vec`/`String`/`Box` allocations wasmtime put into the result slot.
`wasmtime_component_func_post_return` only releases guest-side `cabi_realloc`
buffers; the Rust-allocated host-side copy needs `wasmtime_component_val_delete`
(or per-vec `_delete` siblings).

Fix is paired with #1 — once `ReleaseRustOwned` is wired up, the generator's
`finally` block calls it for every `rets[i]`. Repro: call any composite-result
export 10 000 times and watch RSS.

### 3. `Call` runs `post_return` before the caller has read the result

`ComponentFunction.Call` invokes `post_return` immediately after the function
call, before the user lifts `results[]`. Today wasmtime clones the Rust
`Val` out of guest memory before returning, so the lifted view is stable —
but that's an implementation detail of the current C API, not a contract.
The header is explicit ("after the embedder has finished processing the return
value then this function must be invoked").

Attempted fix in this branch: split `Call` into call + `PostReturn()` and let
the generator emit `try { call → lift } finally { PostReturn → free }`.
Triggers a `panic!("None")` in `crates/c-api/src/store.rs:116:30` on certain
test paths even though wasmtime's Rust API documents a no-op for functions
without a post-return option. Needs a smaller repro to file upstream before
re-attempting.

### 4. `option<tuple<...>>` does not compile

`FunctionEmitter.IsValueType` only treats primitives, enums, and flags as
value types. Tuples and anonymous result/option types are also value types in
the emitted C# (`ValueTuple<...>`, `Wasmtime.Components.Result<T,E>`,
`Wasmtime.Components.Option<T>`), so `LowerOption` falls into the
reference-type branch and emits `var!.ItemN`, which is invalid against
`Nullable<ValueTuple<...>>`.

One-line fix: extend `IsValueType` with `or WitTupleKind or WitResultKind or
WitOptionKind`. Test by adding `export maybe-pair: func(present: bool) ->
option<tuple<u32, string>>;` to the fixture and asserting the round-trip.
(Attempted in this branch but rolled back together with #1/#2/#3 because the
combined diff couldn't keep the test suite green.)

### 5. Type aliases (`type my-list = list<u32>`) generate broken code

`EmitContext.ResolveIndex` returns `MyList` for any named type definition,
but `TypeEmitter.EmitNamedTypes` only emits declarations for `record`,
`enum`, `flags`, and `variant`. Aliases to `list`/`option`/`result`/`tuple`
or another named type produce a reference to a type that's never declared
(`CS0246`).

Two paths: emit the alias as a `using` (`using MyList = ...;` at the top of
the generated file) so the rest of the bindings keep referring to the alias
name; or fall through to structural rendering and ignore the alias name.
The second is a one-liner in `ResolveIndex` (only emit `def.Name` for the
four nominal kinds; otherwise drop into the structural switch).

### 6. Duplicate `EmbeddedResource` for `fixtures.wasm`

`tests/Wasmtime.Tests.csproj` had both an `Update` and an `Include` for the
same file. The `Update` has nothing to update (no glob picks `*.wasm`), so
it's dead code. Drop one of them.

## Should be addressed

### 7. README example references a non-existent `GreeterBindings` fixture

The "Component Model" section in `README.md` was added in `0869856`. It
shows `[ComponentBindings("greeter.wit", world: "host")]` plus a
`HostImports` implementation, but there's no greeter fixture committed.
Either ship a minimal greeter alongside (`tests/Components/greeter-src/`)
or rewrite the example against the existing `FixtureBindings`.

### 8. `AsList` / `AsRecord` shallow-copies retain owner bits

`DecodeValueArray` does `result[i] = array[i]` — a struct copy. With #1
fixed, the copy must scrub whatever ownership marker the new design uses so
that an accidental `Free` on a returned element is a safe no-op rather than
a double-free.

### 10. `RegisterImports` partial-failure recovery

If `DefineFunc` fails for the third out of five imports, the first two
trampolines stay registered on the linker. Document the resulting "linker
must be discarded" contract on `RegisterImports` xmldoc, or track the
registered names and unbind them on failure (the C API may not support the
latter, in which case documenting is the only path).

### 11. WIT case name `none` collides with `Wasmtime.Components.Option<T>.None`

Already mostly under control because every variant case is nested inside the
generated variant type (`Greeting.None`, not bare `None`), but anyone bringing
both into scope via `using static` will hit the ambiguity. Add a short note
in `docs/component-model.md`'s limitations section.

## Recommended

- Diagnostic for `WitUnknownKind` rather than silently emitting `object`.
- `using System.Linq;` and `using System.Collections.Generic;` directives at
  the top of the generated file so emitted code reads more naturally.
- `Debug.Assert` on `Marshal.SizeOf<WasmtimeComponentFunc>()` and on
  `Marshal.SizeOf<WasmtimeComponentValUnion>()` (mirror the Rust-side
  `const _: ()` size assertions).
- Include `ex.GetType().FullName` plus a stack frame in the host-trampoline's
  `wasmtime_error_new` message.

## Process

Each item above must land with a test that fails without the fix and passes
with it. The `/branch-review` rule is "no pre-existing", and these are now
explicitly tracked work — so they belong to this PR thread, not someone
else's.
