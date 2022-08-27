using FluentAssertions;
using System;
using Xunit;

namespace Wasmtime.Tests
{
    public class FuelConsumptionFixture : ModuleFixture
    {
        protected override string ModuleFileName => "FuelConsumption.wat";

        public override Config GetEngineConfig()
        {
            return base.GetEngineConfig()
                .WithFuelConsumption(true);
        }
    }

    public class FuelConsumptionTests : IClassFixture<FuelConsumptionFixture>, IDisposable
    {
        private Store Store { get; set; }

        private Linker Linker { get; set; }

        private FuelConsumptionFixture Fixture { get; }

        public FuelConsumptionTests(FuelConsumptionFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Linker.Define("env", "free", Function.FromCallback(Store, () =>
            {
                // do nothing
            }));
            Linker.Define("env", "expensive", Function.FromCallback(Store, (Caller caller) =>
            {
                caller.ConsumeFuel(100UL);
            }));
        }

        [Fact]
        public void ItConsumesNoFuelBeforeFuelIsAdded()
        {
            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(0UL);
        }

        [Fact]
        public void ItConsumesNoFuelWhenFuelIsAdded()
        {
            Store.AddFuel(1000UL);

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(0UL);
        }

        [Fact]
        public void ItConsumesAddedFuel()
        {
            Store.AddFuel(1000UL);
            var remaining = Store.ConsumeFuel(250UL);
            remaining.Should().Be(750UL);

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(250UL);
        }

        [Fact]
        public void ItCanConsumeZeroFuel()
        {
            Store.AddFuel(1000UL);
            var remaining = Store.ConsumeFuel(0UL);
            remaining.Should().Be(1000UL);

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(0UL);
        }

        [Fact]
        public void ItThrowsOnConsumingTooMuchFuel()
        {
            Store.AddFuel(1000UL);

            Action action = () => Store.ConsumeFuel(2000UL);
            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("not enough fuel remaining in store*");

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(0UL);
        }

        [Fact]
        public void ItConsumesFuelWhenCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.AddFuel(1000UL);

            free.Invoke();
            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(2UL);

            free.Invoke();
            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(4UL);
        }

        [Fact]
        public void ItConsumesFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.AddFuel(1000UL);

            expensive.Invoke();
            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(102UL);
        }

        [Fact]
        public void ItThrowsOnCallingImportMethodIfNoFuelAdded()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("all fuel consumed by WebAssembly*");

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(1UL);
        }

        [Fact]
        public void ItThrowsOnCallingImportMethodIfNotEnoughFuelAdded()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.AddFuel(4UL);

            free.Invoke();
            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(2UL);

            free.Invoke();
            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(4UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("all fuel consumed by WebAssembly*");

            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(5UL);
        }

        [Fact]
        public void ItThrowsWhenConsumingTooMuchFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.AddFuel(50UL);

            Action action = () => expensive.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("not enough fuel remaining in store*");

            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(2UL);
        }

        [Fact]
        public void ItAddsAdditonalFuelAfterCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.AddFuel(4UL);

            free.Invoke();
            var consumed = Store.GetConsumedFuel();
            consumed.Should().Be(2UL);

            free.Invoke();
            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(4UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("all fuel consumed by WebAssembly*");

            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(5UL);

            Store.AddFuel(3UL);

            free.Invoke();
            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(7UL);

            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("all fuel consumed by WebAssembly*");

            consumed = Store.GetConsumedFuel();
            consumed.Should().Be(8UL);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
