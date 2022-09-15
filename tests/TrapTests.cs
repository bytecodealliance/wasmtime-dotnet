using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TrapFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Trap.wat";
    }

    public class TrapTests : IClassFixture<TrapFixture>, IDisposable
    {
        private TrapFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public TrapTests(TrapFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItIncludesAStackTrace()
        {
            Action action = () =>
            {
                var instance = Linker.Instantiate(Store, Fixture.Module);
                var run = instance.GetAction("run");
                run.Should().NotBeNull();
                run();
            };

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Frames.Count == 4 &&
                            e.Frames[0].FunctionName == "third" &&
                            e.Frames[1].FunctionName == "second" &&
                            e.Frames[2].FunctionName == "first" &&
                            e.Frames[3].FunctionName == "run")
                .WithMessage("wasm trap: wasm `unreachable` instruction executed*");
        }

        [Fact]
        public void ItReturnsATrapCodeResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<Result>("run_div_zero");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Should().Be(TrapCode.IntegerDivisionByZero);
        }

        [Fact]
        public void ItReturnsATrapCodeAndBacktraceResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<ResultWithBacktrace>("run_div_zero");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Type.Should().Be(TrapCode.IntegerDivisionByZero);
            result.Trap.Frames.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ItReturnsATrapCodeGenericResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<Result<int>>("run_div_zero_with_result");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Should().Be(TrapCode.IntegerDivisionByZero);
        }

        [Fact]
        public void ItReturnsATrapCodeAndBacktraceGenericResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<ResultWithBacktrace<int>>("run_div_zero_with_result");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Type.Should().Be(TrapCode.IntegerDivisionByZero);
            result.Trap.Frames.Count.Should().BeGreaterThan(0);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
