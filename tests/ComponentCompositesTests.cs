using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public class ComponentCompositesTests
{
    private static byte[] LoadFixture(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"Embedded fixture '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private sealed class Fixture : IDisposable
    {
        public Engine Engine { get; }
        public Component Component { get; }
        public ComponentLinker Linker { get; }
        public Store Store { get; }
        public ComponentInstance Instance { get; }

        public Fixture()
        {
            Engine = new Engine();
            Component = Component.FromBytes(Engine, LoadFixture("fixtures.wasm"));
            Linker = new ComponentLinker(Engine);
            Store = new Store(Engine);

            Store.SetWasiConfiguration(new WasiConfiguration());
            Linker.AddWasiPreview2();

            // The componentize-dotnet-built fixture imports `host-double`; register a passthrough so
            // the runtime-API tests can still instantiate the component without using generated bindings.
            using (var root = Linker.Root())
            {
                root.DefineFunc("host-double", (args, results) =>
                {
                    results[0] = ComponentValue.FromU32(args[0].AsU32() * 2);
                });
            }

            Instance = Linker.Instantiate(Store, Component);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
            Component.Dispose();
            Engine.Dispose();
        }
    }

    [Fact]
    public void Origin_ReturnsRecord()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("origin");
        func.Should().NotBeNull();

        var results = new ComponentValue[1];
        try
        {
            func!.Call(ReadOnlySpan<ComponentValue>.Empty, results);

            results[0].Kind.Should().Be(ComponentValueKind.Record);
            var fields = results[0].AsRecord();
            fields.Should().HaveCount(2);
            fields[0].Name.Should().Be("x");
            fields[0].Value.AsU32().Should().Be(3u);
            fields[1].Name.Should().Be("y");
            fields[1].Value.AsU32().Should().Be(4u);
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void Range_ReturnsList()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("range");
        func.Should().NotBeNull();

        var results = new ComponentValue[1];
        try
        {
            func!.Call(ReadOnlySpan<ComponentValue>.Empty, results);

            results[0].Kind.Should().Be(ComponentValueKind.List);
            var elements = results[0].AsList();
            elements.Should().HaveCount(3);
            elements[0].AsU32().Should().Be(10u);
            elements[1].AsU32().Should().Be(20u);
            elements[2].AsU32().Should().Be(30u);
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void TopPriority_ReturnsEnum()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("top-priority");
        func.Should().NotBeNull();

        var results = new ComponentValue[1];
        try
        {
            func!.Call(ReadOnlySpan<ComponentValue>.Empty, results);

            results[0].Kind.Should().Be(ComponentValueKind.Enum);
            results[0].AsEnum().Should().Be("high");
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void Defaults_ReturnsFlags()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("defaults");
        func.Should().NotBeNull();

        var results = new ComponentValue[1];
        try
        {
            func!.Call(ReadOnlySpan<ComponentValue>.Empty, results);

            results[0].Kind.Should().Be(ComponentValueKind.Flags);
            results[0].AsFlags().Should().BeEquivalentTo(new[] { "read", "write" });
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void Greet_ReturnsVariantWithPayload()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("greet");
        func.Should().NotBeNull();

        var args = new[] { ComponentValue.FromBool(true) };
        var results = new ComponentValue[1];
        try
        {
            func!.Call(args, results);

            results[0].Kind.Should().Be(ComponentValueKind.Variant);
            results[0].AsVariantDiscriminant().Should().Be("formal");
            var payload = results[0].AsVariantPayload();
            payload.Should().NotBeNull();
            payload!.Value.AsString().Should().Be("Sir");
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void SafeDivide_ReturnsOk()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("safe-divide");
        func.Should().NotBeNull();

        var args = new[] { ComponentValue.FromU32(10), ComponentValue.FromU32(2) };
        var results = new ComponentValue[1];
        try
        {
            func!.Call(args, results);

            results[0].Kind.Should().Be(ComponentValueKind.Result);
            results[0].IsOk().Should().BeTrue();
            results[0].AsResultValue()!.Value.AsU32().Should().Be(5u);
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void SafeDivide_ReturnsErr()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("safe-divide");
        func.Should().NotBeNull();

        var args = new[] { ComponentValue.FromU32(10), ComponentValue.FromU32(0) };
        var results = new ComponentValue[1];
        try
        {
            func!.Call(args, results);

            results[0].Kind.Should().Be(ComponentValueKind.Result);
            results[0].IsOk().Should().BeFalse();
            results[0].AsResultValue()!.Value.AsString().Should().Be("division by zero");
        }
        finally
        {
            results[0].Free();
        }
    }

    [Fact]
    public void Find_ReturnsSomeAndNone()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("find");
        func.Should().NotBeNull();

        var some = new ComponentValue[1];
        try
        {
            func!.Call(new[] { ComponentValue.FromU32(42) }, some);
            some[0].Kind.Should().Be(ComponentValueKind.Option);
            some[0].HasOption().Should().BeTrue();
            some[0].AsOption()!.Value.AsString().Should().Be("answer");
        }
        finally
        {
            some[0].Free();
        }

        var none = new ComponentValue[1];
        try
        {
            func!.Call(new[] { ComponentValue.FromU32(0) }, none);
            none[0].HasOption().Should().BeFalse();
            none[0].AsOption().Should().BeNull();
        }
        finally
        {
            none[0].Free();
        }
    }

    [Fact]
    public void Pair_ReturnsTuple()
    {
        using var fixture = new Fixture();
        var func = fixture.Instance.GetFunction("pair");
        func.Should().NotBeNull();

        var results = new ComponentValue[1];
        try
        {
            func!.Call(ReadOnlySpan<ComponentValue>.Empty, results);

            results[0].Kind.Should().Be(ComponentValueKind.Tuple);
            var elements = results[0].AsTuple();
            elements.Should().HaveCount(2);
            elements[0].AsU32().Should().Be(7u);
            elements[1].AsString().Should().Be("seven");
        }
        finally
        {
            results[0].Free();
        }
    }
}
