# Component Model support

This document describes wasmtime-dotnet's support for the [WebAssembly Component
Model][cm-spec] (WASI 0.2). It covers the runtime API in
`Wasmtime.Components`, the Roslyn source generator
(`Wasmtime.Component.SourceGenerators`) that turns WIT files into idiomatic C#
bindings, the build pipeline used to produce the test fixture, and the current
limitations.

[cm-spec]: https://github.com/WebAssembly/component-model

## Architecture

There are three layers, top to bottom:

1. **Generated bindings** — a `partial class` annotated with
   `[ComponentBindings("foo.wit", world: "...")]`. The generator emits
   strongly-typed C# records / enums / variants for the WIT types in that world,
   call wrappers for every export, and an `IImports` interface plus a static
   `RegisterImports` helper for everything the world imports.
2. **Runtime API** — `Wasmtime.Components.Component`,
   `ComponentLinker`/`ComponentLinkerInstance`, `ComponentInstance`,
   `ComponentFunction`, and `ComponentValue`. These are thin SafeHandle-backed
   wrappers around the `wasmtime_component_*` C API and hide all the
   blittable-struct layout work (notably `wasmtime_component_func_t`'s 24-byte
   layout, see commit cf74ac0).
3. **wasmtime C API** — `crates/c-api/include/wasmtime/component/{component,
   func, instance, linker, val}.h`. wasmtime's Rust internals do the canonical
   ABI lifting/lowering; managed code only marshals between C# values and the
   tagged-union `wasmtime_component_val_t`.

The source generator does **not** parse WIT itself. It consumes the JSON IR
produced by `wasm-tools component wit foo.wit --json`, which is committed as
`foo.wit.json` next to `foo.wit`. This re-uses Rust's battle-tested WIT
front-end without dragging a Rust toolchain into csc.

## Quick start

`csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Wasmtime" Version="..." />
  <PackageReference Include="Wasmtime.Component.SourceGenerators"
                    Version="..."
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>

<ItemGroup>
  <AdditionalFiles Include="greeter.wit" />
  <AdditionalFiles Include="greeter.wit.json" />
</ItemGroup>
```

`Program.cs`:

```csharp
using Wasmtime;
using Wasmtime.Components;

[ComponentBindings("greeter.wit", world: "host")]
public partial class GreeterBindings { }

class HostImports : GreeterBindings.IImports
{
    public void Log(string message) => Console.WriteLine(message);
}

using var engine = new Engine();
using var component = Component.FromFile(engine, "greeter.wasm");
using var linker = new ComponentLinker(engine);
using var store = new Store(engine);
store.SetWasiConfiguration(new WasiConfiguration());
linker.AddWasiPreview2();

GreeterBindings.RegisterImports(linker, new HostImports());
var instance = linker.Instantiate(store, component);
var bindings = new GreeterBindings(instance);
```

## WIT → C# type mapping

| WIT             | C#                                           |
| --------------- | -------------------------------------------- |
| `bool`          | `bool`                                       |
| `s8 .. s64`     | `sbyte / short / int / long`                 |
| `u8 .. u64`     | `byte / ushort / uint / ulong`               |
| `f32 / f64`     | `float / double`                             |
| `char`          | `uint` (Unicode scalar value)                |
| `string`        | `string` (UTF-16 ↔ UTF-8 transcoded)         |
| `list<T>`       | `IReadOnlyList<T>`                           |
| `option<T>`     | `T?`                                         |
| `result<T, E>`  | `Wasmtime.Components.Result<T, E>`           |
| `tuple<...>`    | `(T1, T2, ...)` (`ValueTuple`)               |
| `record`        | `sealed record class`                        |
| `enum`          | `enum : byte/ushort/uint`                    |
| `flags`         | `[Flags] enum : byte/ushort/uint/ulong`      |
| `variant`       | `abstract record` + `sealed record` per case |
| `resource`      | **not supported** (see Limitations)          |
| `own<R>`        | not supported                                |
| `borrow<R>`     | not supported                                |

Names are kebab-case in WIT and PascalCase in the generated C#
(`top-priority` → `TopPriority`). Conflicts with C# keywords are escaped with
`@`.

## Test fixture build pipeline

The test fixture under `tests/Components/fixtures-src/` is itself a .NET
component compiled by [`componentize-dotnet`][cdnet] (NativeAOT-LLVM under the
hood). NativeAOT-LLVM has no macOS prebuilts, so the fixture is built inside
an arm64 Linux container:

```bash
./tests/Components/regenerate.sh
```

The script:

1. Compiles the small WAT fixtures (`add.wat`, `hello-string.wat`,
   `host-add.wat`) via `wasm-tools parse`.
2. Builds `fixtures-src/Fixtures.csproj` inside
   `mcr.microsoft.com/dotnet/sdk:10.0` (arm64), overriding the WASI SDK URL
   because componentize-dotnet's MSBuild target hard-codes the x86_64
   download.
3. Runs `wasm-tools component wit fixtures.wit --json` to refresh the JSON IR
   the source generator consumes.

Pre-built `.wasm` artifacts are committed so consumers don't need the Linux
toolchain to run the test suite.

[cdnet]: https://github.com/bytecodealliance/componentize-dotnet

## Limitations

- **`resource` types are not supported.** The wasmtime C API gained the
  `wasmtime_component_resource_*` surface only in v42.0.0; upstream
  wasmtime-dotnet currently pins v35.0.0, where Rust's val.rs has
  `Val::Resource(_) => todo!()`. Standard WASI 0.2 interfaces that internally
  use resources (`wasi:io/streams`, `wasi:filesystem/types`,
  `wasi:sockets/{tcp,udp}`, …) still work because wasmtime native handles those
  resource tables internally — the limitation only affects custom WIT
  `resource` declarations and `own<R>` / `borrow<R>` values that cross the
  managed boundary. Closing this requires upgrading the wasmtime native binary
  to v42+ and is tracked as a follow-up.
- **Async types** — `stream<T>`, `future<T>`, `error-context`, and async
  function declarations are part of WASI 0.3 and are not implemented; they
  weren't part of this work's scope.
- **Single component instance per store** — `wasmtime_component_linker_instantiate`
  errors if called twice on the same store. The wrapper surfaces the wasmtime
  error directly; the API does not currently throw a friendlier
  `InvalidOperationException`.
- **`option<option<T>>`** — generated as `T??`, which is not a valid C# type.
  Nested options need a dedicated `Option<T>` struct in
  `Wasmtime.Components`; not yet emitted.
- **Variant case names** — clashes with C# keywords are guarded only at the
  type-name level (`@`-prefixed); case names like `class`, `default`, etc. are
  not specifically rewritten in `record` declarations and may produce CS9061.
- **Custom `interface` blocks** — the generator currently consumes worlds with
  inline types (the shape produced by simple WIT files and by
  componentize-dotnet output). Cross-package `use` statements and free-standing
  `interface`s are parsed via the JSON IR but emission is unverified.
- **MSBuild auto-generation of `.wit.json`** — currently manual via
  `regenerate.sh`. A proper MSBuild target that runs `wasm-tools` per `.wit`
  file is a follow-up.

## Implementation notes

- `WasmtimeComponentFunc` mirrors a Rust `#[repr(C)]` struct that contains an
  inner anonymous struct, so the layout is 24 bytes (not 16). The Rust side
  enforces this with a `const _: ()` size assertion; we use
  `[StructLayout(LayoutKind.Explicit, Size = 24)]` to match. See commit
  `cf74ac0` — the wrong layout caused wasmtime to return adjacent function
  values (e.g. `top-priority` returned `origin`'s record value) until fixed.
- `ComponentValue.Free()` releases buffers that the managed `From*` factories
  allocated. Values populated by wasmtime (return slots after `Call`) carry
  `ownsHeap = 0` so calling `Free` is a safe no-op; wasmtime itself reclaims
  any nested allocations on the next call's `post_return`.
- Host-defined functions registered with `ComponentLinkerInstance.DefineFunc`
  are kept alive via `GCHandle`, freed by a paired native finalizer when the
  linker is disposed. Exceptions thrown from the C# callback are converted to a
  wasmtime trap via `wasmtime_error_new`.
