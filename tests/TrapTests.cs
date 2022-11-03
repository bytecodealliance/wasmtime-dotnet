using System;
using System.IO;

using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TrapFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Trap.wat";
    }

    /// <summary>
    /// Demonstrates what is required to create a custom result type+builder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct MyCustomResult<T>
        : IFunctionResult<MyCustomResult<T>, T, MyCustomResult<T>.MyCustomResultBuilder>
    {
        public int TrapStackDepth;

        public struct MyCustomResultBuilder
            : IFunctionResultBuilder<MyCustomResult<T>, T>
        {
            public MyCustomResult<T> Create(T value)
            {
                return new MyCustomResult<T>
                {
                    TrapStackDepth = -1,
                };
            }

            public MyCustomResult<T> Create(TrapAccessor accessor)
            {
                return new MyCustomResult<T>
                {
                    TrapStackDepth = accessor.GetFrames().Count,
                };
            }
        }
    }

    public class TrapTests : IClassFixture<TrapFixture>, IDisposable
    {
        private TrapFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        private Action TrapFromHostExceptionCallback { get; set; }

        private Action HostCallback { get; set; }

        public TrapTests(TrapFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);

            Linker.Define("", "host_trap", Function.FromCallback(Store, () => throw new Exception()));

            Linker.Define("", "trap_from_host_exception", Function.FromCallback(
                Store,
                () => TrapFromHostExceptionCallback?.Invoke()));

            Linker.Define("", "call_host_callback", Function.FromCallback(
                Store,
                () => HostCallback?.Invoke()));
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
                .WithMessage("*wasm trap: wasm `unreachable` instruction executed*");
        }

        [Fact]
        public void ItReturnsNullForNestedResults()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<FunctionResult<FunctionResult<int>>>("ok_value");
            run.Should().BeNull();
        }

        [Fact]
        public void ItReturnsOkFromActionResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<ActionResult>("ok");
            var result = run();

            result.Type.Should().Be(ResultType.Ok);

            var trap = () => result.Trap;
            trap.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ItReturnsOkFromFunctionResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<FunctionResult<int>>("ok_value");
            var result = run();

            result.Type.Should().Be(ResultType.Ok);
            result.Value.Should().Be(1);
            ((int)result).Should().Be(1);

            var trap = () => result.Trap;
            trap.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ItReturnsATrapCodeAndBacktraceFromActionResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<ActionResult>("run_div_zero");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Type.Should().Be(TrapCode.IntegerDivisionByZero);
            result.Trap.Frames.Count.Should().Be(2);
            result.Trap.Frames[0].FunctionName.Should().Be("run_div_zero_with_result");
            result.Trap.Frames[1].FunctionName.Should().Be("run_div_zero");
        }

        [Fact]
        public void ItReturnsATrapCodeAndBacktraceFunctionFromResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<FunctionResult<int>>("run_div_zero_with_result");
            var result = run();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Type.Should().Be(TrapCode.IntegerDivisionByZero);
            result.Trap.Frames.Count.Should().Be(1);
            result.Trap.Frames[0].FunctionName.Should().Be("run_div_zero_with_result");
        }

        [Fact]
        public void ItThrowsWhenAccessingValueResultFromTrapResult()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<FunctionResult<int>>("run_div_zero_with_result");
            var result = run();

            var valueDirect = () => result.Value;
            valueDirect.Should().Throw<InvalidOperationException>();

            var valueCast = () => (int)result;
            valueCast.Should().Throw<TrapException>();
        }

        [Fact]
        public void ItThrowsWhenAccessingTrapResultFromOkResult()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<FunctionResult<int>>("ok_value");
            var result = run();

            var valueDirect = () => result.Trap;
            valueDirect.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ItHandlesCustomResultTypeWithOkResult()
        {

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<MyCustomResult<int>>("ok_value");
            var result = run();

            result.TrapStackDepth.Should().Be(-1);
        }

        [Fact]
        public void ItHandlesCustomResultTypeWithTrapResult()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var run = instance.GetFunction<MyCustomResult<int>>("run_div_zero_with_result");
            var result = run();

            result.TrapStackDepth.Should().Be(1);
        }

        [Fact]
        public void ItPassesCallbackTrapCauseAsInnerException()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var callTrap = instance.GetAction("trap_from_host_exception");
            var trapInWasm = instance.GetAction("trap_in_wasm");

            var exceptionToThrow = new IOException("My I/O exception.");

            TrapFromHostExceptionCallback = () => throw exceptionToThrow;

            // Verify that the IOException thrown at the host callback is passed as
            // InnerException to the TrapException thrown on the host-to-wasm transition.
            var action = callTrap;

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.Undefined &&
                    e.InnerException == exceptionToThrow);

            // After that, ensure that when invoking another function that traps in wasm
            // (so it cannot have a cause), the TrapException's InnerException is now null.
            action = trapInWasm;
            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.Unreachable &&
                    e.InnerException == null);

            // Also verify the InnerException is set when using an ActionResult.
            var callTrapAsActionResult = instance.GetFunction<ActionResult>("trap_from_host_exception");
            var result = callTrapAsActionResult();

            result.Type.Should().Be(ResultType.Trap);
            result.Trap.Type.Should().Be(TrapCode.Undefined);
            result.Trap.InnerException.Should().Be(exceptionToThrow);
        }

        [Fact]
        public void ItPassesCallbackTrapCauseAsInnerExceptionOverTwoLevels()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var callTrap = instance.GetAction("trap_from_host_exception");
            var callHostCallback = instance.GetAction("call_host_callback");

            var exceptionToThrow = new IOException("My I/O exception.");

            TrapFromHostExceptionCallback = () => throw exceptionToThrow;
            HostCallback = callTrap;

            // Verify that the IOException is passed as InnerException to the
            // TrapException even after two levels of wasm-to-host transitions.
            var action = callHostCallback;

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.Undefined &&
                    e.InnerException == exceptionToThrow);
        }

        [Fact]
        public void ItPassesCallbackTrapCauseAsInnerExceptionWhenInstantiating()
        {
            var exceptionToThrow = new IOException("My I/O exception.");
            HostCallback = () => throw exceptionToThrow;
            
            var action = () => Linker.Instantiate(Store, Fixture.Module);

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Type == TrapCode.Undefined &&
                    e.InnerException == exceptionToThrow);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
