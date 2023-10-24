using System;
using System.Collections.Generic;
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
        Store.SetFuel(1000000);
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
    public void ItCanGetCachedMemoryFromCaller()
    {
        var memories = new HashSet<Memory>(ReferenceEqualityComparer.Instance);

        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            memories.Add(c.GetMemory("memory"));
            c.GetMemory("none").Should().BeNull();
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
        callback.Invoke();
        callback.Invoke();
        callback.Invoke();
        callback.Invoke();

        // Check that it retrieved the exact same `Memory` object for all calls
        memories.Count.Should().Be(1);
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
    public void ItReturnsNoSpanForNonExistantMemory()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.TryGetMemorySpan<byte>("idontexist", 0, 1, out var span).Should().BeFalse();
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
    public void ItReturnsNoSpanForNonMemoryExport()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.TryGetMemorySpan<byte>("call_callback", 0, 1, out var span).Should().BeFalse();
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
        var memory = instance.GetMemory("memory")!;
        memory.WriteByte(10, 20);

        // Callback checks that value and writes another
        callback.Invoke();

        // Read value back from memory, checking it has been modified
        memory.ReadByte(10).Should().Be(21);
    }

    [Fact]
    public void ItCanReadAndWriteMemorySpanFromCaller()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.TryGetMemorySpan<byte>("memory", 10, 1, out var span).Should().BeTrue();

            span.Length.Should().Be(1);
            span[0].Should().Be(20);
            span[0] = 21;
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        // Write a value into memory
        var memory = instance.GetMemory("memory")!;
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
    public void ItCanGetCachedFunctionFromCaller()
    {
        var functions = new HashSet<Function>(ReferenceEqualityComparer.Instance);

        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            var add = c.GetFunction("add");
            var call = c.GetFunction("call_callback");

            add.Should().NotBe(call);
            functions.Add(add);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
        callback.Invoke();
        callback.Invoke();
        callback.Invoke();
        callback.Invoke();

        // Check that it retrieved the exact same `Function` object for all calls
        functions.Count.Should().Be(1);
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
    public void ItCanSetFuel()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            c.SetFuel(10);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;

        callback.Invoke();
        Store.GetFuel().Should().Be(10);
    }

    [Fact]
    public void ItCanGetFuel()
    {
        Linker.DefineFunction("env", "callback", (Caller c) =>
        {
            // Initial fuel is 100000, minus 2 for the executed wasm
            c.GetFuel().Should().Be(999998);
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var callback = instance.GetFunction("call_callback")!;
        callback.Invoke();
    }

    [Fact]
    public void ItCannotBeConstructedFromNullPointer()
    {
        Action act = () => new Caller(IntPtr.Zero);
        act.Should().Throw<InvalidOperationException>();
    }



    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }
}