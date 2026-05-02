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
    public void Generator_EmitsConstantsOnPartialClass()
    {
        FixtureBindings.WitPath.Should().Be("Components/fixtures.wit");
        FixtureBindings.WitWorld.Should().Be("fixture");
        FixtureBindings.WitSourceLength.Should().BeGreaterThan(0);
    }
}
