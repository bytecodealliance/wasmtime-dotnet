using FluentAssertions;
using System;
using System.Runtime.Intrinsics;
using Xunit;

namespace Wasmtime.Tests
{
    public class FunctionsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Functions.wat";
    }

    public class FunctionTests : IClassFixture<FunctionsFixture>, IDisposable
    {
        const string THROW_MESSAGE = "Test error message for wasmtime dotnet unit tests.";

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public FunctionTests(FunctionsFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Linker.Define("env", "add", Function.FromCallback(Store, (int x, int y) => x + y));
            Linker.Define("env", "swap", Function.FromCallback(Store, (int x, int y) => (y, x)));
            Linker.Define("env", "do_throw", Function.FromCallback(Store, () => throw new Exception(THROW_MESSAGE)));
            Linker.Define("env", "check_string", Function.FromCallback(Store, (Caller caller, int address, int length) =>
            {
                caller.GetMemory("mem").ReadString(caller, address, length).Should().Be("Hello World");
            }));
        }

        private FunctionsFixture Fixture { get; }

        [Fact]
        public void ItBindsImportMethodsAndCallsThemCorrectly()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction(Store, "add");
            var swap = instance.GetFunction(Store, "swap");
            var check = instance.GetFunction(Store, "check_string");

            int x = (int)add.Invoke(Store, 40, 2);
            x.Should().Be(42);
            x = (int)add.Invoke(Store, 22, 5);
            x.Should().Be(27);

            object[] results = (object[])swap.Invoke(Store, 10, 100);
            results.Should().Equal(new object[] { 100, 10 });

            check.Invoke(Store);

            // Collect garbage to make sure delegate function pointers passed to wasmtime are rooted.
            GC.Collect();
            GC.WaitForPendingFinalizers();

            x = (int)add.Invoke(Store, 1970, 50);
            x.Should().Be(2020);

            results = (object[])swap.Invoke(Store, 2020, 1970);
            results.Should().Equal(new object[] { 1970, 2020 });

            check.Invoke(Store);
        }

        [Fact]
        public void ItWrapsArgumentsInValueBox()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction(Store, "add");

            var args = new ValueBox[] { 40, 2 };
            int x = (int)add.Invoke(Store, args.AsSpan());
            x.Should().Be(42);
        }

        [Fact]
        public void ItPropagatesExceptionsToCallersViaTraps()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var thrower = instance.GetFunction(Store, "do_throw");

            Action action = () => thrower.Invoke(Store);

            action
                .Should()
                .Throw<TrapException>()
                // Ideally this should contain a check for the backtrace
                // See: https://github.com/bytecodealliance/wasmtime/issues/1845
                .WithMessage(THROW_MESSAGE + "*");
        }

        [Fact]
        public void ItEchoesV128()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction(Store, "$echo_v128");

            var result = (V128)echo.Invoke(Store, V128.AllBitsSet);

            result
                .Should()
                .Be(V128.AllBitsSet);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
