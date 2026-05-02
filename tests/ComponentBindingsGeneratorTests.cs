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
        FixtureBindings.WitExportCount.Should().Be(8);
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
}
