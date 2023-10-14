using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests;

public class AsyncFixture : ModuleFixture
{
    protected override string ModuleFileName => "Caller.wat";

    public override Config GetEngineConfig()
    {
        return base.GetEngineConfig()
                   .WithAsync(true)
                   .WithFuelConsumption(true);
    }
}

public sealed class AsyncTests
    : IClassFixture<CallerFixture>, IDisposable
{
    public Store Store { get; set; }

    public Linker Linker { get; set; }

    public CallerFixture Fixture { get; }

    public AsyncTests(CallerFixture fixture)
    {
        Fixture = fixture;
        Linker = new Linker(Fixture.Engine);
        Store = new Store(Fixture.Engine);

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

    
}