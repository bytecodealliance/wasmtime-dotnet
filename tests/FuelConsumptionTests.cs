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
                caller.SetFuel(Math.Max(caller.GetFuel(), 100UL) - 100UL);
            }));
        }

        [Fact]
        public void InitialFuelShouldBeZero()
        {
            Store.GetFuel().Should().Be(0UL);
        }

        [Fact]
        public void ItSetsFuel()
        {
            Store.SetFuel(1000UL);
            Store.GetFuel().Should().Be(1000UL);

            Store.SetFuel(1UL);
            Store.GetFuel().Should().Be(1UL);
        }

        [Fact]
        public void ItConsumesFuelWhenCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free").WrapFunc<ActionResult>();

            Store.SetFuel(1000UL);

            free.Invoke().Type.Should().Be(ResultType.Ok);
            Store.GetFuel().Should().Be(998UL);

            free.Invoke().Type.Should().Be(ResultType.Ok);
            Store.GetFuel().Should().Be(996UL);
        }

        [Fact]
        public void ItConsumesFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.SetFuel(1000UL);

            expensive.Invoke();
            Store.GetFuel().Should().Be(898UL);
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

            Store.GetFuel().Should().Be(0UL);
        }

        [Fact]
        public void ItThrowsOnCallingImportMethodIfNotEnoughFuelAdded()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.SetFuel(4UL);

            free.Invoke();
            Store.GetFuel().Should().Be(2UL);

            free.Invoke();
            Store.GetFuel().Should().Be(0UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.GetFuel().Should().Be(0UL);
        }

        [Fact]
        public void ItThrowsWhenConsumingTooMuchFuelFromInsideAnImportMethod()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var expensive = instance.GetFunction("expensive");

            Store.SetFuel(50UL);
            expensive.Invoke();
            Store.GetFuel().Should().Be(0UL);

            Action action = () => expensive.Invoke();
            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("*all fuel consumed by WebAssembly*");
        }

        [Fact]
        public void ItAddsAdditionalFuelAfterCallingImportMethods()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var free = instance.GetFunction("free");

            Store.SetFuel(4UL);

            free.Invoke();
            Store.GetFuel().Should().Be(2UL);

            free.Invoke();
            Store.GetFuel().Should().Be(0UL);

            Action action = () => free.Invoke();
            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.GetFuel().Should().Be(0UL);

            Store.SetFuel(3UL);

            free.Invoke();
            Store.GetFuel().Should().Be(1UL);

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.OutOfFuel)
                .WithMessage("*all fuel consumed by WebAssembly*");

            Store.GetFuel().Should().Be(0UL);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
