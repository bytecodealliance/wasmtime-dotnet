using System;
using Xunit;

namespace Wasmtime.Tests;

public sealed class EngineTests
{
    [Fact]
    public void ItCannotBeAccessedOnceDisposed()
    {
        var engine = new Engine();

        engine.Dispose();

        Assert.Throws<ObjectDisposedException>(() => engine.NativeHandle);
        Assert.Throws<ObjectDisposedException>(() => engine.IncrementEpoch());
    }

    [Fact]
    public void ItLeavesNullReferenceExceptionsCatchable()
    {
        using var engine = new Engine();
        using var module = Module.FromText(engine, "test", "(module (func (export \"run\")))");
        using var linker = new Linker(engine);
        using var store = new Store(engine);
        // Wasmtime sets up its trap handling for a thread when WebAssembly first runs on it.
        linker.Instantiate(store, module).GetAction("run")!();
        string missing = null;

        Assert.Throws<NullReferenceException>(() => missing.Length);
    }
}