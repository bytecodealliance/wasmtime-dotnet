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
                caller.GetMemory("mem").ReadString(address, length).Should().Be("Hello World");
            }));
        }

        private FunctionsFixture Fixture { get; }

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
        public void ItWrapsASimpleAction()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var noop = instance.GetAction("noop");
            noop.Should().NotBeNull();
            noop();
        }

        [Fact]
        public void ItWrapsArgumentsInValueBox()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction("add");

            var args = new ValueBox[] { 40, 2 };
            int x = (int)add.Invoke(args.AsSpan());
            x.Should().Be(42);
        }

        [Fact]
        public void ItGetsArgumentsFromGenericSpecification()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var add = instance.GetFunction<int, int, int>("add");
            add.Should().NotBeNull();

            int x = add(40, 2);
            x.Should().Be(42);
        }

        [Fact]
        public void ItReturnsNullForInvalidTypeSpecification()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            instance.GetFunction<double, int, int>("add").Should().BeNull();
            instance.GetFunction<int, double, int>("add").Should().BeNull();
            instance.GetFunction<int, int, double>("add").Should().BeNull();
            instance.GetFunction<int, int, int, int>("add").Should().BeNull();
            instance.GetAction<int, int>("add").Should().BeNull();
        }

        [Fact]
        public void ItGetsArgumentsFromGenericSpecificationWithMultipleReturns()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var swap = instance.GetFunction<int, int, (int, int)>("swap");
            swap.Should().NotBeNull();

            (int x, int y) = swap(100, 10);
            x.Should().Be(10);
            y.Should().Be(100);
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

        [Fact]
        public void ItEchoesInt32()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<int, int>("$echo_i32");
            echo.Should().NotBeNull();

            var result = echo.Invoke(42);
            result.Should().Be(42);
        }

        [Fact]
        public void ItEchoesInt64()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<long, long>("$echo_i64");
            echo.Should().NotBeNull();

            var result = echo.Invoke(42);
            result.Should().Be(42);
        }

        [Fact]
        public void ItEchoesFloat32()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<float, float>("$echo_f32");
            echo.Should().NotBeNull();

            var result = echo.Invoke(42);
            result.Should().Be(42);
        }

        [Fact]
        public void ItEchoesFloat64()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<double, double>("$echo_f64");
            echo.Should().NotBeNull();

            var result = echo.Invoke(42);
            result.Should().Be(42);
        }

        [Fact]
        public void ItEchoesV128()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<V128, V128>("$echo_v128");
            echo.Should().NotBeNull();

            var result = echo.Invoke(V128.AllBitsSet);
            result.Should().Be(V128.AllBitsSet);
        }

        [Fact]
        public void ItEchoesFuncref()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var func = instance.GetFunction("$echo_funcref");
            var echo = func.WrapFunc<Function, Function>();
            echo.Should().NotBeNull();

            var result = echo.Invoke(func);

            result.Should().NotBeNull();
            result.CheckTypeSignature(typeof(Function), typeof(Function)).Should().BeTrue();
        }

        [Fact]
        public void ItEchoesExternref()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<object, object>("$echo_externref");
            echo.Should().NotBeNull();

            var obj = new object();

            var result = echo.Invoke(obj);

            result.Should().NotBeNull();
            result.Should().BeSameAs(obj);
        }

        [Fact]
        public void ItEchoesExternrefString()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<object, object>("$echo_externref");
            echo.Should().NotBeNull();

            var str = "Hello Wasmtime";

            var result = echo.Invoke(str);

            result.Should().NotBeNull();
            result.Should().BeSameAs(str);
        }

        [Fact]
        public void ItReturnsTwoItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int)>("$echo_tuple2");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2));
        }

        [Fact]
        public void ItReturnsThreeItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int, int)>("$echo_tuple3");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2, 3));
        }

        [Fact]
        public void ItReturnsFourItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int, int, float)>("$echo_tuple4");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2, 3, 3.141f));
        }

        [Fact]
        public void ItReturnsFiveItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int, int, float, double)>("$echo_tuple5");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2, 3, 3.141f, 2.71828));
        }

        [Fact]
        public void ItReturnsSixItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int, int, float, double, int)>("$echo_tuple6");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2, 3, 3.141f, 2.71828, 6));
        }

        [Fact]
        public void ItReturnsSevenItemTuple()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<(int, int, int, float, double, int, int)>("$echo_tuple7");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be((1, 2, 3, 3.141f, 2.71828, 6, 7));
        }

        [Fact]
        public void ItReturnsNullForVeryLongTuples()
        {
            // Note that this test is about the current limitations of the system. It's possible
            // to support longer tuples, in which case this test will need modifying.

            var instance = Linker.Instantiate(Store, Fixture.Module);
            instance.GetFunction<(int, int, int, float, double, int, int, int)>("$echo_tuple8")
                .Should()
                .BeNull();
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
