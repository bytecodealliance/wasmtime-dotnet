using System.IO;
using System.Reflection;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

[ComponentBindings("Components/fixtures.wit", world: "fixture")]
public partial class FixtureBindings
{
}

public class ComponentBindingsGeneratorTests
{
    [Fact]
    public void Generator_EmitsConstantsFromJsonIr()
    {
        FixtureBindings.WitPath.Should().Be("Components/fixtures.wit");
        FixtureBindings.WitWorld.Should().Be("fixture");
        // 4 named types (point, priority, permissions, greeting) + 4 anonymous
        // (list<u32>, result<u32, string>, option<string>, tuple<u32, string>)
        FixtureBindings.WitTypeCount.Should().Be(8);
        FixtureBindings.WitImportCount.Should().Be(4);
        FixtureBindings.WitExportCount.Should().Be(10);
    }

    [Fact]
    public void Generator_EmitsExportNames()
    {
        FixtureBindings.WitExportNames.Should().BeEquivalentTo(new[]
        {
            "origin",
            "range",
            "top-priority",
            "defaults",
            "greet",
            "safe-divide",
            "find",
            "pair",
            "square",
            "translate",
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Generator_EmitsNamedTypes()
    {
        FixtureBindings.WitTypeNames.Should().Contain(new[] { "point", "priority", "permissions", "greeting" });
    }

    [Fact]
    public void Generator_EmitsRecord()
    {
        var p = new FixtureBindings.Point(3, 4);
        p.X.Should().Be(3u);
        p.Y.Should().Be(4u);
    }

    [Fact]
    public void Generator_EmitsEnum()
    {
        var v = FixtureBindings.Priority.High;
        v.Should().Be(FixtureBindings.Priority.High);
        ((byte)v).Should().Be(2);
    }

    [Fact]
    public void Generator_EmitsFlags()
    {
        var flags = FixtureBindings.Permissions.Read | FixtureBindings.Permissions.Write;
        flags.HasFlag(FixtureBindings.Permissions.Read).Should().BeTrue();
        flags.HasFlag(FixtureBindings.Permissions.Execute).Should().BeFalse();
    }

    [Fact]
    public void Generator_EmitsVariantWithPayload()
    {
        FixtureBindings.Greeting g = new FixtureBindings.Greeting.Formal("Sir");
        g.Should().BeOfType<FixtureBindings.Greeting.Formal>();
        ((FixtureBindings.Greeting.Formal)g).Value.Should().Be("Sir");
    }

    [Fact]
    public void Generator_EmitsVariantWithoutPayload()
    {
        FixtureBindings.Greeting g = new FixtureBindings.Greeting.None();
        g.Should().BeOfType<FixtureBindings.Greeting.None>();
    }

    private static FixtureBindings CreateBindings(out Engine engine, out Component component, out ComponentLinker linker, out Store store)
    {
        var bytes = LoadFixtureBytes("fixtures.wasm");

        engine = new Engine();
        component = Component.FromBytes(engine, bytes);
        linker = new ComponentLinker(engine);
        store = new Store(engine);

        store.SetWasiConfiguration(new WasiConfiguration());
        linker.AddWasiPreview2();

        var instance = linker.Instantiate(store, component);
        return new FixtureBindings(instance);
    }

    [Fact]
    public void Generator_PrimitiveExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            b.Square(7).Should().Be(49u);
            b.Square(0).Should().Be(0u);
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_RecordExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            var p = b.Origin();
            p.Should().Be(new FixtureBindings.Point(3, 4));
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_EnumExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            b.TopPriority().Should().Be(FixtureBindings.Priority.High);
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_FlagsExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            b.Defaults().Should().Be(FixtureBindings.Permissions.Read | FixtureBindings.Permissions.Write);
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_VariantExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            var formal = b.Greet(true);
            formal.Should().BeOfType<FixtureBindings.Greeting.Formal>();
            ((FixtureBindings.Greeting.Formal)formal).Value.Should().Be("Sir");

            var casual = b.Greet(false);
            casual.Should().BeOfType<FixtureBindings.Greeting.Casual>();
            ((FixtureBindings.Greeting.Casual)casual).Value.Should().Be("hi");
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_ListExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            b.Range().Should().Equal(10u, 20u, 30u);
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_OptionExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            b.Find(42).Should().Be("answer");
            b.Find(0).Should().BeNull();
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_ResultExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            var ok = b.SafeDivide(10, 2);
            ok.IsOk.Should().BeTrue();
            ok.Value.Should().Be(5u);

            var err = b.SafeDivide(10, 0);
            err.IsOk.Should().BeFalse();
            err.Error.Should().Be("division by zero");
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_RecordRoundTrip_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            // Host constructs the record, ships it to the component, component returns a transformed record.
            var moved = b.Translate(new FixtureBindings.Point(1, 2), 10, 20);
            moved.Should().Be(new FixtureBindings.Point(11, 22));
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    [Fact]
    public void Generator_TupleExport_EndToEnd()
    {
        var b = CreateBindings(out var engine, out var component, out var linker, out var store);
        try
        {
            var (n, s) = b.Pair();
            n.Should().Be(7u);
            s.Should().Be("seven");
        }
        finally
        {
            store.Dispose(); linker.Dispose(); component.Dispose(); engine.Dispose();
        }
    }

    private static byte[] LoadFixtureBytes(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"Fixture '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
