using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

[ComponentBindings("nested-option.wit", world: "nested")]
public partial class NestedBindings
{
}

public class NestedOptionTests
{
    [Fact]
    public void Option_Some_RoundTrips()
    {
        var some = Option<uint?>.Some(42);
        some.HasValue.Should().BeTrue();
        some.Value.Should().Be(42u);

        var someNull = Option<uint?>.Some(null);
        someNull.HasValue.Should().BeTrue();
        someNull.Value.Should().BeNull();
    }

    [Fact]
    public void Option_None_HasNoValue()
    {
        var none = Option<uint?>.None;
        none.HasValue.Should().BeFalse();
    }
}
