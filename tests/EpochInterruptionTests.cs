using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests;

public sealed class EpochInterruptionFixture : ModuleFixture
{
    protected override string ModuleFileName => "Interrupt.wat";

    public override Config GetEngineConfig()
    {
        return base.GetEngineConfig()
            .WithEpochInterruption(true);
    }
}

public sealed class EpochInterruptionTests : IClassFixture<EpochInterruptionFixture>, IDisposable
{
    public Store Store { get; set; }

    public Linker Linker { get; set; }

    public EpochInterruptionFixture Fixture { get; }

    public EpochInterruptionTests(EpochInterruptionFixture fixture)
    {
        Fixture = fixture;
        Linker = new Linker(Fixture.Engine);
        Store = new Store(Fixture.Engine);
    }

    [Fact]
    public void ItCanInterruptInfiniteLoop()
    {
        Store.SetEpochDeadline(1);

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var run = instance.GetFunction("run");

        var action = () =>
        {
            using var cts = new CancellationTokenSource(100);
            using var tokenRegistration = cts.Token.Register(Fixture.Engine.IncrementEpoch);

            run.Invoke();
        };

        action.Should()
            .Throw<TrapException>()
            .WithMessage("*wasm trap: interrupt*");
    }

    /// <summary>
    /// Runs the given body while a background thread advances the engine epoch
    /// </summary>
    private void WhileEpochAdvances(Action body)
    {
        using var stop = new CancellationTokenSource();
        var ticker = new Thread(() =>
        {
            while (!stop.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(5)))
            {
                Fixture.Engine.IncrementEpoch();
            }
        })
        {
            IsBackground = true
        };

        ticker.Start();

        try
        {
            body();
        }
        finally
        {
            stop.Cancel();
            ticker.Join();
        }
    }

    [Fact]
    public void ItRenewsTheDeadlineUntilTheCallbackThrows()
    {
        var invocations = 0;
        Store observedStore = null;
        var exceptionToThrow = new OperationCanceledException("the guest was cancelled");

        Store.SetEpochDeadline(1);
        Store.SetEpochDeadlineCallback(store =>
        {
            observedStore = store;

            if (++invocations == 3)
            {
                throw exceptionToThrow;
            }

            // Resume for one more tick, so the deadline is reached again.
            return 1;
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var run = instance.GetFunction("run");

        WhileEpochAdvances(() =>
        {
            var action = () => run.Invoke();

            action.Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException == exceptionToThrow)
                .WithMessage("*the guest was cancelled*");
        });

        invocations.Should().Be(3);
        observedStore.Should().BeSameAs(Store);
    }

    [Fact]
    public void ItReplacesAPreviouslySetCallback()
    {
        var replacedInvocations = 0;
        var invocations = 0;

        Store.SetEpochDeadline(1);
        Store.SetEpochDeadlineCallback(_ =>
        {
            replacedInvocations++;
            return 1;
        });

        Store.SetEpochDeadlineCallback(_ =>
        {
            invocations++;
            throw new InvalidOperationException("stop");
        });

        var instance = Linker.Instantiate(Store, Fixture.Module);
        var run = instance.GetFunction("run");

        WhileEpochAdvances(() =>
        {
            var action = () => run.Invoke();

            action.Should().Throw<WasmtimeException>().WithMessage("*stop*");
        });

        invocations.Should().Be(1);
        replacedInvocations.Should().Be(0);
    }

    [Fact]
    public void ItThrowsForANullCallback()
    {
        Assert.Throws<ArgumentNullException>(() => Store.SetEpochDeadlineCallback(null!));
    }

    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }
}
