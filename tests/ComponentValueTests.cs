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
}
