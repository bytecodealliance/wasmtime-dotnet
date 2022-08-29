using FluentAssertions;
using System;
using Xunit;

namespace Wasmtime.Tests
{
    public class LinkerFunctionsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Functions.wat";
    }

    public class LinkerFunctionTests : IClassFixture<LinkerFunctionsFixture>, IDisposable
    {
        const string THROW_MESSAGE = "Test error message for wasmtime dotnet unit tests.";

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public LinkerFunctionTests(LinkerFunctionsFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Linker.DefineFunction("env", "add", (int x, int y) => x + y);
            Linker.DefineFunction("env", "swap", (int x, int y) => (y, x));
            Linker.DefineFunction("env", "do_throw", () => throw new Exception(THROW_MESSAGE));
            Linker.DefineFunction("env", "check_string", (Caller caller, int address, int length) =>
            {
                caller.GetMemory("mem").ReadString(address, length).Should().Be("Hello World");
            });
        }

        private LinkerFunctionsFixture Fixture { get; }

        [Fact]
        public void ItBindsImportMethodsAndCallsThemCorrectly()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction("add");
            var swap = instance.GetFunction("swap");
            var check = instance.GetFunction("check_string");

            int x = (int)add.Invoke(40, 2);
            x.Should().Be(42);
            x = (int)add.Invoke(22, 5);
            x.Should().Be(27);

            object[] results = (object[])swap.Invoke(10, 100);
            results.Should().Equal(new object[] { 100, 10 });

            check.Invoke();

            // Collect garbage to make sure delegate function pointers passed to wasmtime are rooted.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            x = (int)add.Invoke(1970, 50);
            x.Should().Be(2020);

            results = (object[])swap.Invoke(2020, 1970);
            results.Should().Equal(new object[] { 1970, 2020 });

            check.Invoke();
        }

        [Fact]
        public void ItPropagatesExceptionsToCallersViaTraps()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var thrower = instance.GetFunction("do_throw");

            Action action = () => thrower.Invoke();

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
