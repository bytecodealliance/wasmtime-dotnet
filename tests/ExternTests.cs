using System.Diagnostics;
using Xunit;

namespace Wasmtime.Tests;

public class ExternTests
{
    [Fact]
    public void ExternFunc_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternFunc), default);
    }

    [Fact]
    public void ExternTable_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternTable), default);
    }

    [Fact]
    public void ExternMemory_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternMemory), default);
    }

    [Fact]
    public void ExternInstance_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternInstance), default);
    }

    [Fact]
    public void ExternGlobal_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternGlobal), default);
    }

    [Fact]
    public void ExternUnion_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(ExternUnion), default);
        Assert.Equal(default(Extern), default);
    }

    [Fact]
    public void Extern_StaticCheck()
    {
        // An assertion is made in the static constructor. Doing this forces the static constructor to run
        Assert.Equal(default(Extern), default);
    }
}