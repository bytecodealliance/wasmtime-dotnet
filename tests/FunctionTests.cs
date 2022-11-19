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
            Linker.Define("env", "add_reflection", Function.FromCallback(Store, (Delegate)((int x, int y) => x + y)));
            Linker.Define("env", "swap", Function.FromCallback(Store, (int x, int y) => (y, x)));
            Linker.Define("env", "do_throw", Function.FromCallback(Store, () => throw new Exception(THROW_MESSAGE)));
            Linker.Define("env", "check_string", Function.FromCallback(Store, (Caller caller, int address, int length) =>
            {
                caller.GetMemory("mem").ReadString(address, length).Should().Be("Hello World");
            }));

            Linker.Define("env", "return_i32", Function.FromCallback(Store, GetBoundFuncIntDelegate()));

            Linker.Define("env", "return_15_values", Function.FromCallback(Store, () =>
            {
                return (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            }));

            Linker.Define("env", "accept_15_values", Function.FromCallback(Store,
                (int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15) =>
                {
                    (i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14, i15)
                        .Should().Be((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
                }));

            var echoMultipleValuesFunc = EchoMultipleValues;
            Linker.Define("env", "pass_through_multiple_values1", Function.FromCallback(Store, echoMultipleValuesFunc));
            Linker.Define("env", "pass_through_multiple_values2", Function.FromCallback(Store, (EchoMultipleValuesCustomDelegate)EchoMultipleValues));

            Linker.Define("env", "pass_through_v128", Function.FromCallback(Store, (V128 v128) => v128));

            Func<int> GetBoundFuncIntDelegate()
            {
                // Get a delegate that is bound over an argument.
                // See #159
                var getLengthDelegate = GetLength;
                var getLengthMethod = getLengthDelegate.Method;

                string str = "abc";
                return (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), str, getLengthMethod);

                int GetLength(string s)
                {
                    return s.Length;
                }
            }

            (long, double, object) EchoMultipleValues(long l, double d, object o)
            {
                return (l, d, o);
            }
        }

        private FunctionsFixture Fixture { get; }

        [Fact]
        public void ItBindsImportMethodsAndCallsThemCorrectly()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction("add");
            var addReflection = instance.GetFunction("add_reflection");
            var swap = instance.GetFunction("swap");
            var check = instance.GetFunction("check_string");

            int x = (int)add.Invoke(40, 2);
            x.Should().Be(42);
            x = (int)add.Invoke(22, 5);
            x.Should().Be(27);

            x = (int)addReflection.Invoke(40, 2);
            x.Should().Be(42);
            x = (int)addReflection.Invoke(22, 5);
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
        public void ItPropagatesExceptionsToCallersViaErrors()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var thrower = instance.GetFunction("do_throw");

            Action action = () => thrower.Invoke();

            action
                .Should()
                .Throw<WasmtimeException>()
                // Ideally this should contain a check for the backtrace
                // See: https://github.com/bytecodealliance/wasmtime/issues/1845
                .WithMessage("*" + THROW_MESSAGE + "*");
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
            var echo = instance.GetFunction<float, FunctionResult<float>>("$echo_f32");
            echo.Should().NotBeNull();

            var result = (float)echo.Invoke(42);
            result.Should().Be(42);
        }

        [Fact]
        public void ItEchoesFloat64()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<double, FunctionResult<double>>("$echo_f64");
            echo.Should().NotBeNull();

            var result = (double)echo.Invoke(42);
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
        public void ItEchoesMultipleValuesFromFuncDelegate()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<long, double, object, (long, double, object)>("pass_through_multiple_values1");
            echo.Should().NotBeNull();

            var result = echo(1, 2, "3");
            result.Should().Be((1, 2, "3"));
        }

        [Fact]
        public void ItEchoesMultipleValuesFromCustomDelegate()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<long, double, object, (long, double, object)>("pass_through_multiple_values2");
            echo.Should().NotBeNull();

            var result = echo(1, 2, "3");
            result.Should().Be((1, 2, "3"));
        }

        [Fact]
        public void ItEchoesV128FromFuncDelegate()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var passThrough = instance.GetFunction<V128, V128>("pass_through_v128");
            passThrough.Should().NotBeNull();

            var result = passThrough.Invoke(V128.AllBitsSet);
            result.Should().Be(V128.AllBitsSet);
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

        [Fact]
        public void ItReturnsInt32WithBoundDelegate()
        {
            // Test for issue #159: It should be possible to defined a Delegate that is bound with
            // a first argument.
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var echo = instance.GetFunction<int>("return_i32");
            echo.Should().NotBeNull();

            var result = echo.Invoke();
            result.Should().Be(3);
        }

        [Fact]
        public void ItReturnsAndAccepts15Values()
        {
            // Verify that nested levels of ValueTuple are handled correctly. Returning 15
            // values means that a ValueTuple<..., ValueTuple<..., ValueTuple<...>>> is used.
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var action = instance.GetAction("get_and_pass_15_values");
            action.Should().NotBeNull();

            action.Invoke();
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }

        public delegate (long, double, object) EchoMultipleValuesCustomDelegate(long l, double d, object o);
    }
}
