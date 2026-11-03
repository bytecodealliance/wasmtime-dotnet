using System;
using Xunit;

namespace Wasmtime.Tests;

public class Wasi2Tests
{
    [Fact]
    public void ItSetsWasi2Config()
    {
        using var engine = new Engine();
        using var store  = new Store(engine);

        var config = new Wasi2Configuration();
        config.WithInheritedStandardInput();
        config.WithInheritedStandardOutput();
        config.WithInheritedStandardError();

        store.SetWasiConfiguration(config);
    }
}