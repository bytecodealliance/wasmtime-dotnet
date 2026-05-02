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
}
