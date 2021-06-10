using FluentAssertions;
using System;
using Xunit;

namespace Wasmtime.Tests
{
    public class FunctionThunkingFixture : ModuleFixture
    {
        protected override string ModuleFileName => "FunctionThunking.wat";
    }

    public class FunctionThunkingTests : IClassFixture<FunctionThunkingFixture>, IDisposable
    {
        const string THROW_MESSAGE = "Test error message for wasmtime dotnet unit tests.";

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public FunctionThunkingTests(FunctionThunkingFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            var context = Store.Context;
            Linker.Define("env", "add", Function.FromCallback(context, (int x, int y) => x + y));
            Linker.Define("env", "swap", Function.FromCallback(context, (int x, int y) => (y, x)));
            Linker.Define("env", "do_throw", Function.FromCallback(context, () => throw new Exception(THROW_MESSAGE)));
            Linker.Define("env", "check_string", Function.FromCallback(context, (Caller caller, int address, int length) =>
            {
                caller.GetMemory("mem").ReadString(caller.Context, address, length).Should().Be("Hello World");
            }));
        }

        private FunctionThunkingFixture Fixture { get; }

        [Fact]
        public void ItBindsImportMethodsAndCallsThemCorrectly()
        {
            var context = Store.Context;
            var instance = Linker.Instantiate(context, Fixture.Module);
            var add = instance.GetFunction(context, "add");
            var swap = instance.GetFunction(context, "swap");
            var check = instance.GetFunction(context, "check_string");

            int x = (int)add.Invoke(context, 40, 2);
            x.Should().Be(42);
            x = (int)add.Invoke(context, 22, 5);
            x.Should().Be(27);

            object[] results = (object[])swap.Invoke(context, 10, 100);
            results.Should().Equal(new object[] { 100, 10 });

            check.Invoke(context);

            // Collect garbage to make sure delegate function pointers passed to wasmtime are rooted.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            x = (int)add.Invoke(context, 1970, 50);
            x.Should().Be(2020);

            results = (object[])swap.Invoke(context, 2020, 1970);
            results.Should().Equal(new object[] { 1970, 2020 });

            check.Invoke(context);
        }

        [Fact]
        public void ItPropagatesExceptionsToCallersViaTraps()
        {
            var context = Store.Context;
            var instance = Linker.Instantiate(context, Fixture.Module);
            var thrower = instance.GetFunction(context, "do_throw");

            Action action = () => thrower.Invoke(Store.Context);

            action
                .Should()
                .Throw<TrapException>()
                // Ideally this should contain a check for the backtrace
                // See: https://github.com/bytecodealliance/wasmtime/issues/1845
                .WithMessage(THROW_MESSAGE + "*");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
