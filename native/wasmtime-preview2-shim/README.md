# WASI Preview2 Shim

This crate builds a small native shim that runs WASI 0.2 (preview2) components
in-process using Wasmtime. The shim is invoked from .NET via P/Invoke.

## Requirements

- Rust 1.86+ (Wasmtime 35 requires a newer toolchain)

## Build

```
cd native/wasmtime-preview2-shim
cargo build --release
```

Artifacts:

- macOS: `target/release/libwasmtime_preview2_shim.dylib`
- Linux: `target/release/libwasmtime_preview2_shim.so`

## Loading from .NET

The managed wrapper expects the library name `wasmtime_preview2_shim`.
Ensure the compiled library is on the dynamic loader search path or copied
next to the .NET application.

Example (macOS):

```
export DYLD_LIBRARY_PATH=/path/to/native/wasmtime-preview2-shim/target/release:$DYLD_LIBRARY_PATH
```

Example (Linux):

```
export LD_LIBRARY_PATH=/path/to/native/wasmtime-preview2-shim/target/release:$LD_LIBRARY_PATH
```

## Core module extraction (library components)

Library components that do not export `wasi:cli/run` can be handled by extracting
their main core module and using the existing Wasmtime .NET core APIs.

The shim exposes `wasmtime_preview2_extract_core_module` for this purpose.
