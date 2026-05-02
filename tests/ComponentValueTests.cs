using System.Runtime.InteropServices;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public class ComponentValueTests
{
    [Fact]
    public void Layout_MatchesNativeSize()
    {
        Marshal.SizeOf<ComponentValue>().Should().Be(32);
    }

    [Fact]
    public void Bool_RoundTrips()
    {
        var t = ComponentValue.FromBool(true);
        t.Kind.Should().Be(ComponentValueKind.Bool);
        t.AsBool().Should().BeTrue();

        var f = ComponentValue.FromBool(false);
        f.AsBool().Should().BeFalse();
    }

    [Fact]
    public void U32_RoundTrips()
    {
        var v = ComponentValue.FromU32(uint.MaxValue);
        v.Kind.Should().Be(ComponentValueKind.U32);
        v.AsU32().Should().Be(uint.MaxValue);
    }

    [Fact]
    public void S64_RoundTrips()
    {
        var v = ComponentValue.FromS64(long.MinValue);
        v.Kind.Should().Be(ComponentValueKind.S64);
        v.AsS64().Should().Be(long.MinValue);
    }

    [Fact]
    public void F64_RoundTrips()
    {
        var v = ComponentValue.FromF64(3.14159265358979);
        v.Kind.Should().Be(ComponentValueKind.F64);
        v.AsF64().Should().Be(3.14159265358979);
    }

    [Fact]
    public void Char_RoundTrips()
    {
        var v = ComponentValue.FromChar(0x1F600); // 😀
        v.Kind.Should().Be(ComponentValueKind.Char);
        v.AsChar().Should().Be(0x1F600u);
    }

    [Fact]
    public void AccessorRejectsWrongKind()
    {
        var v = ComponentValue.FromU32(42);
        Assert.Throws<System.InvalidOperationException>(() => v.AsBool());
        Assert.Throws<System.InvalidOperationException>(() => v.AsS32());
    }

    [Fact]
    public void String_AsciiRoundTrips()
    {
        var v = ComponentValue.FromString("hello");
        try
        {
            v.Kind.Should().Be(ComponentValueKind.String);
            v.AsString().Should().Be("hello");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void String_EmptyRoundTrips()
    {
        var v = ComponentValue.FromString(string.Empty);
        try
        {
            v.Kind.Should().Be(ComponentValueKind.String);
            v.AsString().Should().BeEmpty();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void String_UnicodeRoundTrips()
    {
        var input = "Привет, 🌍! 日本語";
        var v = ComponentValue.FromString(input);
        try
        {
            v.AsString().Should().Be(input);
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void String_FreeIsIdempotent()
    {
        var v = ComponentValue.FromString("x");
        v.Free();
        v.Free();
    }

    [Fact]
    public void String_FromNullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromString(null!));
    }

    [Fact]
    public void Free_OnPrimitiveIsNoOp()
    {
        var v = ComponentValue.FromU32(7);
        v.Free();
        v.AsU32().Should().Be(7u);
    }

    [Fact]
    public void Enum_RoundTrips()
    {
        var v = ComponentValue.FromEnum("high");
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Enum);
            v.AsEnum().Should().Be("high");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Enum_FromNullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromEnum(null!));
    }

    [Fact]
    public void Flags_RoundTrips()
    {
        var v = ComponentValue.FromFlags(new[] { "read", "write", "execute" });
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Flags);
            v.AsFlags().Should().BeEquivalentTo(new[] { "read", "write", "execute" }, opts => opts.WithStrictOrdering());
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Flags_EmptyRoundTrips()
    {
        var v = ComponentValue.FromFlags(System.Array.Empty<string>());
        try
        {
            v.AsFlags().Should().BeEmpty();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Flags_FromNullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromFlags(null!));
    }

    [Fact]
    public void Flags_FromNullElementThrowsAndCleansUp()
    {
        Assert.Throws<System.ArgumentException>(() => ComponentValue.FromFlags(new string[] { "first", null! }));
    }

    [Fact]
    public void List_OfPrimitivesRoundTrips()
    {
        var v = ComponentValue.FromList(new[]
        {
            ComponentValue.FromU32(1),
            ComponentValue.FromU32(2),
            ComponentValue.FromU32(3),
        });
        try
        {
            v.Kind.Should().Be(ComponentValueKind.List);
            var elements = v.AsList();
            elements.Should().HaveCount(3);
            elements[0].AsU32().Should().Be(1u);
            elements[1].AsU32().Should().Be(2u);
            elements[2].AsU32().Should().Be(3u);
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void List_OfStringsRoundTripsAndFreesRecursively()
    {
        var v = ComponentValue.FromList(new[]
        {
            ComponentValue.FromString("alpha"),
            ComponentValue.FromString("beta"),
        });
        try
        {
            var elements = v.AsList();
            elements[0].AsString().Should().Be("alpha");
            elements[1].AsString().Should().Be("beta");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void List_EmptyRoundTrips()
    {
        var v = ComponentValue.FromList(System.Array.Empty<ComponentValue>());
        try
        {
            v.AsList().Should().BeEmpty();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Tuple_RoundTrips()
    {
        var v = ComponentValue.FromTuple(new[]
        {
            ComponentValue.FromString("answer"),
            ComponentValue.FromU32(42),
            ComponentValue.FromBool(true),
        });
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Tuple);
            var elements = v.AsTuple();
            elements.Should().HaveCount(3);
            elements[0].AsString().Should().Be("answer");
            elements[1].AsU32().Should().Be(42u);
            elements[2].AsBool().Should().BeTrue();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void List_FromNullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromList(null!));
    }

    [Fact]
    public void Record_RoundTrips()
    {
        var v = ComponentValue.FromRecord(new[]
        {
            new RecordField("name", ComponentValue.FromString("Alice")),
            new RecordField("age", ComponentValue.FromU32(30)),
        });
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Record);
            var fields = v.AsRecord();
            fields.Should().HaveCount(2);
            fields[0].Name.Should().Be("name");
            fields[0].Value.AsString().Should().Be("Alice");
            fields[1].Name.Should().Be("age");
            fields[1].Value.AsU32().Should().Be(30u);
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Record_EmptyRoundTrips()
    {
        var v = ComponentValue.FromRecord(System.Array.Empty<RecordField>());
        try
        {
            v.AsRecord().Should().BeEmpty();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Record_FromNullThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromRecord(null!));
    }

    [Fact]
    public void Record_NullFieldNameRollsBack()
    {
        Assert.Throws<System.ArgumentException>(() => ComponentValue.FromRecord(new[]
        {
            new RecordField("first", ComponentValue.FromU32(1)),
            new RecordField(null!, ComponentValue.FromU32(2)),
        }));
    }

    [Fact]
    public void Variant_WithPayloadRoundTrips()
    {
        var v = ComponentValue.FromVariant("formal", ComponentValue.FromString("Sir"));
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Variant);
            v.AsVariantDiscriminant().Should().Be("formal");
            var payload = v.AsVariantPayload();
            payload.Should().NotBeNull();
            payload!.Value.AsString().Should().Be("Sir");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Variant_WithoutPayloadRoundTrips()
    {
        var v = ComponentValue.FromVariant("none");
        try
        {
            v.AsVariantDiscriminant().Should().Be("none");
            v.AsVariantPayload().Should().BeNull();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Variant_NullDiscriminantThrows()
    {
        Assert.Throws<System.ArgumentNullException>(() => ComponentValue.FromVariant(null!));
    }

    [Fact]
    public void Option_NoneRoundTrips()
    {
        var v = ComponentValue.FromOption(null);
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Option);
            v.HasOption().Should().BeFalse();
            v.AsOption().Should().BeNull();
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Option_SomeRoundTrips()
    {
        var v = ComponentValue.FromOption(ComponentValue.FromU32(7));
        try
        {
            v.HasOption().Should().BeTrue();
            v.AsOption()!.Value.AsU32().Should().Be(7u);
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Result_OkRoundTrips()
    {
        var v = ComponentValue.FromOk(ComponentValue.FromString("done"));
        try
        {
            v.Kind.Should().Be(ComponentValueKind.Result);
            v.IsOk().Should().BeTrue();
            v.AsResultValue()!.Value.AsString().Should().Be("done");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Result_ErrRoundTrips()
    {
        var v = ComponentValue.FromErr(ComponentValue.FromString("nope"));
        try
        {
            v.IsOk().Should().BeFalse();
            v.AsResultValue()!.Value.AsString().Should().Be("nope");
        }
        finally
        {
            v.Free();
        }
    }

    [Fact]
    public void Result_OkWithoutPayload()
    {
        var v = ComponentValue.FromOk();
        try
        {
            v.IsOk().Should().BeTrue();
            v.AsResultValue().Should().BeNull();
        }
        finally
        {
            v.Free();
        }
    }
}
