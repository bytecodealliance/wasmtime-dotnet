using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests; 

public class EpochInterruptionFixture : ModuleFixture
{
    protected override string ModuleFileName => "Interrupt.wat";

    public override Config GetEngineConfig() {
        return base.GetEngineConfig()
            .WithEpochInterruption(true);
    }
}

public class EpochInterruptionTests : IClassFixture<EpochInterruptionFixture>, IDisposable {
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
    public void ItCanInterruptInfiniteLoop() {
        Store.SetEpochDeadline(1);
        
        var instance = Linker.Instantiate(Store, Fixture.Module);
        var run = instance.GetFunction(Store, "run");

        var action = () => {
            using (var timer = new Timer(state => Fixture.Engine.IncrementEpoch())) {
                try {
                    timer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(Timeout.Infinite));
                    run.Invoke(Store);
                }
                catch (TrapException trap) {
                    throw new TimeoutException("Invocation timed out", trap);
                }
            }
        };

        action.Should()
            .Throw<TimeoutException>();
    }
        
    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }
}