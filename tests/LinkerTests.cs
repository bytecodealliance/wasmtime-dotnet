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
    public void CloneCarriesAllowShadowing()
    {
        Linker.AllowShadowing = true;

        using var clone = Linker.Clone();

        clone.DefineFunction("", "hello", () => { });
        clone.DefineFunction("", "hello", () => { });
    }

    [Fact]
    public void CloneCarriesDefinitions()
    {
        var called = 0;
        Linker.DefineFunction("", "hello", () => { called++; });

        using var clone = Linker.Clone();
        clone.GetFunction(Store, "", "hello")!.Invoke();

        Assert.Equal(1, called);
    }

    [Fact]
    public void CloneCarriesWasi()
    {
        Linker.DefineWasi();

        using var clone = Linker.Clone();

        Assert.Throws<WasmtimeException>(() => clone.DefineWasi());
    }

    [Fact]
    public void CloneDisallowsShadowingByDefault()
    {
        using var clone = Linker.Clone();

        clone.DefineFunction("", "hello", () => { });
        Assert.Throws<WasmtimeException>(() => clone.DefineFunction("", "hello", () => { }));
    }

    [Fact]
    public void CloneDoesNotShareDefinitionsAddedToTheClone()
    {
        using var clone = Linker.Clone();
        clone.DefineFunction("", "hello", () => { });

        Assert.NotNull(clone.GetFunction(Store, "", "hello"));
        Assert.Null(Linker.GetFunction(Store, "", "hello"));
    }

    [Fact]
    public void CloneDoesNotShareDefinitionsAddedToTheOriginal()
    {
        using var clone = Linker.Clone();
        Linker.DefineFunction("", "hello", () => { });

        Assert.NotNull(Linker.GetFunction(Store, "", "hello"));
        Assert.Null(clone.GetFunction(Store, "", "hello"));
    }

    [Fact]
    public void CloneOutlivesTheOriginal()
    {
        var called = 0;
        var linker = new Linker(Fixture.Engine);
        linker.DefineFunction("", "hello", () => { called++; });

        using var clone = linker.Clone();
        linker.Dispose();

        GC.Collect();
        GC.WaitForPendingFinalizers();

        clone.GetFunction(Store, "", "hello")!.Invoke();

        Assert.Equal(1, called);
    }

    [Fact]
    public void CloneThrowsWhenDisposed()
    {
        var linker = new Linker(Fixture.Engine);
        linker.Dispose();

        Assert.Throws<ObjectDisposedException>(() => linker.Clone());
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
    public void DefineUnknownImportsAsDefaultValuesDefinesDefaultValues()
    {
        using var module = Module.FromText(Fixture.Engine, "test", "(module (import \"env\" \"f\" (func (result i32))) (import \"env\" \"g\" (global i32)) (func (export \"f\") (result i32) call 0) (func (export \"g\") (result i32) global.get 0))");

        Linker.DefineUnknownImportsAsDefaultValues(Store, module);

        var instance = Linker.Instantiate(Store, module);
        Assert.Equal(0, instance.GetFunction<int>("f")!());
        Assert.Equal(0, instance.GetFunction<int>("g")!());
    }

    [Fact]
    public void DefineUnknownImportsAsDefaultValuesThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.DefineUnknownImportsAsDefaultValues(Store, null!));
    }

    [Fact]
    public void DefineUnknownImportsAsDefaultValuesThrowsWithNullStore()
    {
        using var module = Module.FromText(Fixture.Engine, "test", "(module)");

        Assert.Throws<ArgumentNullException>(() => Linker.DefineUnknownImportsAsDefaultValues(null!, module));
    }

    [Fact]
    public void DefineUnknownImportsAsTrapsDefinesFunctionsThatTrap()
    {
        using var module = Module.FromText(Fixture.Engine, "test", "(module (import \"env\" \"f\" (func)) (func (export \"run\") call 0))");

        Linker.DefineUnknownImportsAsTraps(module);

        var run = Linker.Instantiate(Store, module).GetAction("run")!;
        var exception = Assert.Throws<WasmtimeException>(() => run());
        Assert.Contains("env::f", exception.Message);
    }

    [Fact]
    public void DefineUnknownImportsAsTrapsThrowsWithNullModule()
    {
        Assert.Throws<ArgumentNullException>(() => Linker.DefineUnknownImportsAsTraps(null!));
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