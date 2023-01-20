using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests;

public class CallerFixture : ModuleFixture
{
    protected override string ModuleFileName => "Caller.wat";

    public override Config GetEngineConfig()
    {
        return base.GetEngineConfig()
                   .WithFuelConsumption(true);
    }
}

public class CallerTests : IClassFixture<CallerFixture>, IDisposable
{
    public Store Store { get; set; }

    public Linker Linker { get; set; }

    public CallerFixture Fixture { get; }

    public CallerTests(CallerFixture fixture)
    {
        Fixture = fixture;
        Linker = new Linker(Fixture.Engine);
        Store = new Store(Fixture.Engine);

        Store.AddFuel(1000000);
    }

    [Fact]
    public void ItCanGetMemoryFromCaller()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetMemory("memory").Should().NotBeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItReturnsNullForNonExistantMemory()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetMemory("idontexist").Should().BeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItReturnsNullForNonMemoryExport()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetMemory("call_callback").Should().BeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItCanReadAndWriteMemoryFromCaller()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            var mem = c.GetMemory("memory");

            mem.ReadByte(10).Should().Be(20);
            mem.WriteByte(10, 21);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        // Write a value into memory
        var memory = instance.GetMemory("memory");
        memory.WriteByte(10, 20);

        // Callback checks that value and writes another
        callback.Invoke();

        // Read value back from memory, checking it has been modified
        memory.ReadByte(10).Should().Be(21);
    }


    [Fact]
    public void ItCanGetFunctionFromCaller()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetFunction("add").Should().NotBeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItReturnsNullForNonExistantFunction()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetFunction("idontexist").Should().BeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItReturnsNullForNonFunctionExport()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetFunction("memory").Should().BeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }

    [Fact]
    public void ItCanCallFunctionFromCaller()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            var result = c.GetFunction("add").WrapFunc<int, int, int>().Invoke(10, 20);
            result.Should().Be(30);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }


    [Fact]
    public void ItCanGetData()
    {
        var data = new List<int> { 1, 2, 3 };
        Store.SetData(data);

        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            var d = (List<int>)c.GetData()!;
            d.Add(4);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();

        data.Count.Should().Be(4);
        data[3].Should().Be(4);
    }

    [Fact]
    public void ItCanSetData()
    {
        var data = new List<int> { 1, 2, 3 };
        Store.SetData(data);

        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.SetData(new[] { 42f });
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();

        var d = (float[])Store.GetData()!;
        d.Should().NotBeNull();
        d.Length.Should().Be(1);
        d[0].Should().Be(42f);
    }


    [Fact]
    public void ItCanConsumeFuel()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.ConsumeFuel(10).Should().Be(1000000 - (10 + 2));
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();

        // 10 is consumed by the explicit fuel consumption
        // 2 is consumed by the rest of the WASM which executes behind the scenes in this test
        Store.GetConsumedFuel().Should().Be(10 + 2);
    }

    [Fact]
    public void ItCanAddFuel()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.AddFuel(2);
            c.ConsumeFuel(0).Should().Be(1000000);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();

        // 2 is consumed by the WASM which executes behind the scenes in this test
        Store.GetConsumedFuel().Should().Be(2);
    }

    [Fact]
    public void ItCanGetConsumedFuel()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.GetConsumedFuel().Should().Be(2);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
    }


    [Fact]
    public void ItCannotBeAccessedAfterDisposal()
    {
        Caller stash = null!;

        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            stash = c;
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();

        var act = () => stash.GetMemory("memory");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ItCannotBeConstructedFromNullPointer()
    {
        var act = () => new Caller(IntPtr.Zero);
        act.Should().Throw<InvalidOperationException>();
    }



    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }
}