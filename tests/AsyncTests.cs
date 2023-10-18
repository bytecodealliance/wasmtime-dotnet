using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests;

public class AsyncFixture : ModuleFixture
{
    protected override string ModuleFileName => "Async.wat";

    public override Config GetEngineConfig()
    {
        return base.GetEngineConfig()
                   .WithAsync(true)
                   .WithFuelConsumption(true);
    }
}

public sealed class AsyncTests
    : IClassFixture<AsyncFixture>, IDisposable
{
    public Store Store { get; set; }

    public Linker Linker { get; set; }

    public AsyncFixture Fixture { get; }

    public AsyncTests(AsyncFixture fixture)
    {
        Fixture = fixture;
        Linker = new Linker(Fixture.Engine)
        {
            AllowShadowing = true
        };
        Store = new Store(Fixture.Engine);

        Linker.DefineAsyncFunction("", "no_args", () => Task.CompletedTask);
        Linker.DefineAsyncFunction("", "one_arg", (int x) => Task.CompletedTask);
        //Linker.DefineAsyncFunction("", "no_args_one_result", async () => 0);

        Store.AddFuel(1000000);
    }

    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }

    [Fact]
    public void ItCanSetConfigAsyncSupport()
    {
        new Config()
            .WithAsync(false)
            .WithAsync(true)
            .Should().NotBeNull();
    }

    [Fact]
    public void ItCanSetConfigAsyncStackSize()
    {
        new Config()
           .WithAsyncStackSize(1234567890)
           .Should().NotBeNull();
    }

    [Fact]
    public void AddAsyncToLinkerUntyped()
    {
        Linker.DefineAsyncFunction("module", "name", (c, a, r) => { }, Array.Empty<ValueKind>(), Array.Empty<ValueKind>());
    }

    [Fact]
    public async Task InvokeAsyncNoArgsNoResults()
    {
        var success = false;

        Linker.DefineAsyncFunction("", "no_args", async () =>
        {
            await Task.Delay(100);
            success = true;
        });

        var instance = await Linker.InstantiateAsync(Store, Fixture.Module);
        Assert.NotNull(instance);

        var func = instance.GetFunction("call_no_args")?.WrapAction();
        func.Should().NotBeNull();

        var timer = new Stopwatch();
        timer.Start();
        {
            await func!();
        }
        timer.Stop();
        Assert.True(timer.ElapsedMilliseconds > 50, "didn't delay for long enough");

        Assert.True(success, "didn't set flag");
    }

    [Fact]
    public async Task InvokeAsyncCallerNoArgNoResults()
    {
        var success = false;

        Linker.DefineAsyncFunction("", "no_args", caller =>
        {
            var store = caller.Store;
            Assert.NotNull(store);

            // cannot use caller inside an async function (ref struct), so async
            // work has to be split off and not reference caller directly
            var func = async () =>
            {
                await Task.Delay(100);
                success = true;
            };

            return func();
        });

        var instance = await Linker.InstantiateAsync(Store, Fixture.Module);
        Assert.NotNull(instance);

        var func = instance.GetFunction("call_no_args")?.WrapAction();
        func.Should().NotBeNull();

        var timer = new Stopwatch();
        timer.Start();
        {
            await func!();
        }
        timer.Stop();
        Assert.True(timer.ElapsedMilliseconds > 50, "didn't delay for long enough");

        Assert.True(success, "didn't set flag");
    }

    [Fact]
    public async Task InvokeAsyncOneArgNoResults()
    {
        var success = false;

        Linker.DefineAsyncFunction("", "one_arg", async (int duration) =>
        {
            await Task.Delay(duration);
            success = true;
        });

        var instance = await Linker.InstantiateAsync(Store, Fixture.Module);
        Assert.NotNull(instance);

        var func = instance.GetFunction("call_one_arg")?.WrapAction<int>();
        func.Should().NotBeNull();

        var timer = new Stopwatch();
        timer.Start();
        {
            await func!(200);
        }
        timer.Stop();
        Assert.True(timer.ElapsedMilliseconds > 150, "didn't delay for long enough");

        Assert.True(success, "didn't set flag");
    }

    [Fact]
    public async Task InvokeAsyncCallerOneArgNoResults()
    {
        var success = false;

        Linker.DefineAsyncFunction("", "one_arg", (Caller Caller, int duration) =>
        {
            var func = async () =>
            {
                await Task.Delay(duration);
                success = true;
            };

            return func();
        });

        var instance = await Linker.InstantiateAsync(Store, Fixture.Module);
        Assert.NotNull(instance);

        var func = instance.GetFunction("call_one_arg")?.WrapAction<int>();
        func.Should().NotBeNull();

        var timer = new Stopwatch();
        timer.Start();
        {
            await func!(200);
        }
        timer.Stop();
        Assert.True(timer.ElapsedMilliseconds > 150, "didn't delay for long enough");

        Assert.True(success, "didn't set flag");
    }

    //[Fact]
    //public async Task InvokeAsyncNoArgsOneResult()
    //{
    //    var success = false;

    //    Linker.DefineAsyncFunction("", "no_args_one_result", async () =>
    //    {
    //        await Task.Delay(100);
    //        success = true;
    //        return 42;
    //    });

    //    var instance = await Linker.InstantiateAsync(Store, Fixture.Module);
    //    Assert.NotNull(instance);

    //    var func = instance.GetFunction("call_no_args_one_result")?.WrapFunc<int>();
    //    func.Should().NotBeNull();

    //    var timer = new Stopwatch();
    //    timer.Start();
    //    {
    //        Assert.Equal(42, await func!());
    //    }
    //    timer.Stop();
    //    Assert.True(timer.ElapsedMilliseconds > 50, "didn't delay for long enough");

    //    Assert.True(success, "didn't set flag");
    //}
}