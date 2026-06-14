using System;
using Xunit;

namespace Wasmtime.Tests;

public sealed class LinkerFixture : ModuleFixture
{
    protected override string ModuleFileName => "FunctionExports.wat";
}

public sealed class LinkerTests
    : IClassFixture<LinkerFixture>, IDisposable
{
    private Store Store { get; }
    private Linker Linker { get; }

    public LinkerTests(LinkerFixture fixture)
    {
        Fixture = fixture;
        Linker = new Linker(Fixture.Engine);
        Store = new Store(Fixture.Engine);
    }

    public LinkerFixture Fixture { get; }

    public void Dispose()
    {
        Linker.Dispose();
        Store.Dispose();
    }

    [Fact]
    public void ItThrowsWithNullEngine()
    {
        Assert.Throws<ArgumentNullException>(() => new Linker(null!));
    }

    [Fact]
    public void DefineThrowsWithNullStore()
    {
        Assert.Throws<ArgumentException>(() => Linker.Define("module", "name", Function.Null));
    }

    [Fact]
    public void DefineThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.Define(null!, "name", new Global(Store, ValueKind.Int32, 1, Mutability.Immutable)));
    }

    [Fact]
    public void DefineThrowsWithNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.Define("module", null!, new Global(Store, ValueKind.Int32, 1, Mutability.Immutable)));
    }

    [Fact]
    public void DefineModuleThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.DefineModule(Store, null!));
    }

    [Fact]
    public void DefineModuleThrowsWithNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.DefineModule(null!, Fixture.Module));
    }

    [Fact]
    public void DefineWasiTwiceThrows()
    {
        Linker.DefineWasi();
        Assert.Throws<WasmtimeException>(() => Linker.DefineWasi());
    }

    [Fact]
    public void GetDefaultFunctionThrowsWithNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetDefaultFunction(null!, "name"));
    }

    [Fact]
    public void GetDefaultFunctionThrowsWithNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetDefaultFunction(Store, null!));
    }

    [Fact]
    public void GetTableThrowsWithNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetTable(null!, "module", "name"));
    }

    [Fact]
    public void GetTableThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetTable(Store, null!, "name"));
    }

    [Fact]
    public void GetTableThrowsWithNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetTable(Store, "module", null!));
    }

    [Fact]
    public void GetMemoryThrowsWithNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetMemory(null!, "module", "name"));
    }

    [Fact]
    public void GetMemoryThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetMemory(Store, null!, "name"));
    }

    [Fact]
    public void GetMemoryThrowsWithNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetMemory(Store, "module", null!));
    }

    [Fact]
    public void GetGlobalThrowsWithNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetGlobal(null!, "module", "name"));
    }

    [Fact]
    public void GetGlobalThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetGlobal(Store, null!, "name"));
    }

    [Fact]
    public void GetGlobalThrowsWithNullName()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.GetGlobal(Store, "module", null!));
    }

    [Fact]
    public void DefineInstanceThrowsWithNullStore()
    {
        var instance = new Instance(Store, Fixture.Module);
        Assert.Throws<ArgumentNullException>(() => Linker.DefineInstance(null!, "name", instance));
    }

    [Fact]
    public void DefineInstanceThrowsWithNullName()
    {
        var instance = new Instance(Store, Fixture.Module);
        Assert.Throws<ArgumentNullException>(() => Linker.DefineInstance(Store, null!, instance));
    }

    [Fact]
    public void DefineInstanceThrowsWithNullInstance()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.DefineInstance(Store, "name", null!));
    }
}