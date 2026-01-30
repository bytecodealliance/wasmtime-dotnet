# WASI Preview2 Component Runner (Shim)

This repository includes an optional native shim for running WASI 0.2 components
in-process. It pairs a small Rust `cdylib` with a managed wrapper:

- Rust shim: `native/wasmtime-preview2-shim`
- C# wrapper: `src/Preview2ComponentRunner.cs`

## Build the native shim

The shim uses Wasmtime 35 and requires Rust 1.86+.

```
cd native/wasmtime-preview2-shim
cargo build --release
```

Add the resulting library to the runtime loader path:

- macOS: `libwasmtime_preview2_shim.dylib`
- Linux: `libwasmtime_preview2_shim.so`

## Run a component

```
using Wasmtime;

var exitCode = Preview2ComponentRunner.Run(
    componentPath: "dotnet.wasm",
    args: new[] { "arg1", "arg2" },
    environment: new Dictionary<string, string>
    {
        ["DOTNET_EnableDiagnostics"] = "0"
    },
    preopens: new[]
    {
        new PreopenDirectory(".", ".")
    },
    inheritStdio: true,
    inheritEnvironment: true,
    inheritNetwork: true
);
```

The runner wires `wasi:cli/run` and returns the component exit code.

## Library components (no `wasi:cli/run`)

Library-style .NET components do not export `wasi:cli/run`. To invoke their
exported C-ABI functions in-process, extract the main core module and use the
existing Wasmtime .NET core APIs:

```
using Wasmtime;

ComponentCoreExtractor.ExtractMainModule(
    componentPath: "dotnet.wasm",
    outputPath: "core-module.wasm");

using var config = new Config().WithWasmThreads(true);
using var engine = new Engine(config);
using var module = Module.FromFile(engine, "core-module.wasm");
using var store = new Store(engine);

store.SetWasiConfiguration(
    new WasiConfiguration()
        .WithInheritedStandardOutput()
        .WithInheritedStandardError()
        .WithPreopenedDirectory(".", "."));

using var linker = new Linker(engine);
linker.DefineWasi();
linker.DefineWasiPreview2Stubs();

using var instance = linker.Instantiate(store, module);
var alloc = instance.GetFunction<int, int>("alloc");
var dealloc = instance.GetAction<int, int>("dealloc");
```

The extracted core module imports `wasi_snapshot_preview1`, so the core WASI
linker (`DefineWasi`) is required. Some extracted modules also import:

- `wasi_snapshot_preview1.adapter_close_badfd`
- `wasi_snapshot_preview1.adapter_open_badfd`
- WASI preview2 `[resource-drop]` functions (no-op stubs are sufficient)

`DefineWasiPreview2Stubs` provides these stubs for common preview2 interfaces.
