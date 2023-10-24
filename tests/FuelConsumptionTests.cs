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
                checked
                {
                    caller.Fuel -= 100UL;
                }
            }));
        }

        [Fact]
        public void ItHasNoFuelBeforeFuelIsAdded()
        {
            Store.Fuel.Should().Be(0UL);
        }

        [Fact]
        public void ItCanSetAndGetFuel()
        {
            Store.Fuel = 1000UL;
            Store.Fuel.Should().Be(1000UL);
        }

        [Fact]
        public void ItCanAddFuel()
        {
            Store.Fuel = 1000UL;
            Store.Fuel += 1000UL;

            Store.Fuel.Should().Be(2000UL);
        }
        [Fact]
        public void ItCanRemoveFuel()
        {
            Store.Fuel = 1000UL;
            Store.Fuel -= 500UL;

            Store.Fuel.Should().Be(500UL);
        }

        [Fact]
        public void ItConsumesFuelWhenCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free").WrapFunc<ActionResult>();

            Store.Fuel = 1000UL;

            free.Invoke().Type.Should().Be(ResultType.Ok);
            Store.Fuel.Should().Be(1000UL - 2UL);

            free.Invoke().Type.Should().Be(ResultType.Ok);
            Store.Fuel.Should().Be(1000UL - 4UL);
        }

        [Fact]
        public void ItConsumesFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.Fuel = 1000UL;

            expensive.Invoke();
            Store.Fuel.Should().Be(1000UL - 102UL);
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
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.Fuel.Should().Be(0UL);
        }

        [Fact]
        public void ItThrowsOnCallingImportMethodIfNotEnoughFuelAdded()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.Fuel = 4UL;

            free.Invoke();
            Store.Fuel.Should().Be(2UL);

            free.Invoke();
            Store.Fuel.Should().Be(0UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.Fuel.Should().Be(0UL);
        }

        [Fact]
        public void ItThrowsWhenConsumingTooMuchFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.Fuel = 50UL;

            Action action = () => expensive.Invoke();
            action
                .Should()
                .Throw<WasmtimeException>()
                .WithInnerException<OverflowException>();

            Store.Fuel.Should().Be(48UL);
        }

        [Fact]
        public void ItAddsAdditonalFuelAfterCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.Fuel = 4UL;

            free.Invoke();
            Store.Fuel.Should().Be(2UL);

            free.Invoke();
            Store.Fuel.Should().Be(0UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.Fuel += 3UL;

            free.Invoke();
            Store.Fuel.Should().Be(1UL);

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.Fuel.Should().Be(0UL);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
