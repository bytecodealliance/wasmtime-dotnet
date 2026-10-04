using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public sealed class ComponentExtendedValueTests
{
    private static ComponentValue RoundTrip(ComponentValue value, bool owned)
    {
        using var scope = new ComponentValueMarshaller.AllocationScope();
        var pointer = scope.Allocate(ComponentValueMarshaller.ValueSize);
        if (owned) ComponentValueMarshaller.WriteOwned(value, pointer);
        else ComponentValueMarshaller.Write(value, pointer, scope);
        try { return ComponentValueMarshaller.Read(pointer); }
        finally { if (owned) ComponentValueNative.wasmtime_component_val_delete(pointer); }
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0xD7FFu)]
    [InlineData(0xE000u)]
    [InlineData(0xFFFFu)]
    [InlineData(0x10000u)]
    [InlineData(0x1F600u)]
    [InlineData(0x10FFFFu)]
    public void ItRoundTripsUnicodeScalars(uint scalar)
    {
        var rune = new Rune(scalar);
        foreach (var owned in new[] { false, true })
        {
            RoundTrip(ComponentValue.Char(scalar), owned).AsChar().Should().Be(scalar);
            RoundTrip(ComponentValue.Char(rune), owned).AsRune().Should().Be(rune);
            if (scalar <= char.MaxValue)
                RoundTrip(ComponentValue.Char((char)scalar), owned).AsChar().Should().Be(scalar);
        }
    }

    [Theory]
    [InlineData(0xD800u)]
    [InlineData(0xDFFFu)]
    [InlineData(0x110000u)]
    [InlineData(uint.MaxValue)]
    public void ItRejectsInvalidUnicodeScalars(uint scalar)
    {
        Action create = () => ComponentValue.Char(scalar);
        create.Should().Throw<ArgumentOutOfRangeException>();
        if (scalar <= char.MaxValue)
        {
            Action createFromChar = () => ComponentValue.Char((char)scalar);
            createFromChar.Should().Throw<ArgumentOutOfRangeException>();
        }
        using var scope = new ComponentValueMarshaller.AllocationScope();
        var pointer = scope.Allocate(ComponentValueMarshaller.ValueSize);
        Marshal.WriteByte(pointer, (byte)ComponentValueKind.Char);
        Marshal.WriteInt32(pointer + ComponentValueMarshaller.ValuePayloadOffset, unchecked((int)scalar));
        Action read = () => ComponentValueMarshaller.Read(pointer);
        read.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItRoundTripsNestedVariantsAndArbitraryFlagCounts(bool owned)
    {
        var flags = Enumerable.Range(0, 100).Select(i => "flag-" + i).ToArray();
        var value = ComponentValue.Variant("outer",
            ComponentValue.Some(ComponentValue.List(new[]
            {
                ComponentValue.Variant("empty"),
                ComponentValue.Variant("scalar", ComponentValue.Char(0x1F600)),
                ComponentValue.Variant("access", ComponentValue.Flags(flags)),
            })));
        var result = RoundTrip(value, owned);
        result.AsVariant().Should().Be("outer");
        var items = result.Payload!.Payload!.AsList();
        items[0].AsVariant().Should().Be("empty");
        items[0].Payload.Should().BeNull();
        items[1].Payload!.AsChar().Should().Be(0x1F600);
        items[2].Payload!.AsFlags().Should().Equal(flags);
        RoundTrip(ComponentValue.Flags(Array.Empty<string>()), owned).AsFlags().Should().BeEmpty();
    }

    [Fact]
    public void ItCopiesFlagsAndRejectsInvalidInputsAndWrongAccessors()
    {
        var names = new[] { "first" };
        var flags = ComponentValue.Flags(names);
        names[0] = "changed";
        flags.AsFlags().Should().Equal("first");
        Action duplicates = () => ComponentValue.Flags(new[] { "first", "first" });
        duplicates.Should().Throw<ArgumentException>();
        Action nullName = () => ComponentValue.Flags(new[] { (string)null! });
        nullName.Should().Throw<ArgumentException>();
        Action nullNames = () => ComponentValue.Flags(null!);
        nullNames.Should().Throw<ArgumentNullException>();
        Action nullCase = () => ComponentValue.Variant(null!);
        nullCase.Should().Throw<ArgumentNullException>();
        Action wrongChar = () => flags.AsChar();
        wrongChar.Should().Throw<InvalidOperationException>();
        Action wrongVariant = () => flags.AsVariant();
        wrongVariant.Should().Throw<InvalidOperationException>();
        Action wrongFlags = () => ComponentValue.Char(0).AsFlags();
        wrongFlags.Should().Throw<InvalidOperationException>();
    }
}
