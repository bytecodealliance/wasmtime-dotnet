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
}