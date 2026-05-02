using System.IO;
using System.Reflection;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public class ComponentEndToEndTests
{
    private static byte[] LoadFixture(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"Embedded fixture '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void AddComponent_LoadsAndCalls()
    {
        var bytes = LoadFixture("add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);
        using var linker = new ComponentLinker(engine);
        using var store = new Store(engine);

        var instance = linker.Instantiate(store, component);

        var function = instance.GetFunction("add");
        function.Should().NotBeNull();

        var args = new[]
        {
            ComponentValue.FromU32(40),
            ComponentValue.FromU32(2),
        };
        var results = new ComponentValue[1];

        function!.Call(args, results);

        results[0].Kind.Should().Be(ComponentValueKind.U32);
        results[0].AsU32().Should().Be(42u);
    }

    [Fact]
    public void Component_GetExport_FindsAdd()
    {
        var bytes = LoadFixture("add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);

        using var export = component.GetExport("add");
        export.Should().NotBeNull();

        using var missing = component.GetExport("missing");
        missing.Should().BeNull();
    }

    [Fact]
    public void Instance_GetFunction_ReturnsNullForMissing()
    {
        var bytes = LoadFixture("add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);
        using var linker = new ComponentLinker(engine);
        using var store = new Store(engine);

        var instance = linker.Instantiate(store, component);

        var missing = instance.GetFunction("does-not-exist");
        missing.Should().BeNull();
    }

    [Fact]
    public void Component_SerializeRoundTrip()
    {
        var bytes = LoadFixture("add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);

        var serialized = component.Serialize();
        serialized.Should().NotBeEmpty();

        using var roundTripped = Component.Deserialize(engine, serialized);
        using var export = roundTripped.GetExport("add");
        export.Should().NotBeNull();
    }
}
