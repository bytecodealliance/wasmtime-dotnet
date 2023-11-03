using System;
using System.Linq;
using FluentAssertions;
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

            Linker.Define("env", "return_i32", Function.FromCallback(Store, GetBoundFuncIntDelegate()));

            Linker.Define("env", "return_15_values", Function.FromCallback(Store, (_, p, r) =>
                {
                    p.Length.Should().Be(0);
                    r.Length.Should().Be(15);
                    for (int i = 0; i < 15; i++)
                    {
                        r[i] = i;
                    }
                },
                Array.Empty<ValueKind>(),
                Enumerable.Repeat(ValueKind.Int32, 15).ToArray()
            ));

            Linker.Define("env", "accept_15_values", Function.FromCallback(Store, (_, p, r) =>
                {
                    p.Length.Should().Be(15);
                    r.Length.Should().Be(0);
                    for (int i = 0; i < 15; i++)
                    {
                        p[i].AsInt32().Should().Be(i);
                    }
                },
                Enumerable.Repeat(ValueKind.Int32, 15).ToArray(),
                Array.Empty<ValueKind>()
            ));

            var emptyFunc = Function.FromCallback(Store, () => { });
            Linker.Define("env", "return_all_types", Function.FromCallback(Store, (_, p, r) =>
            {
                p.Length.Should().Be(0);
                r.Length.Should().Be(7);
                r[0] = 1;
                r[1] = 2L;
                r[2] = 3f;
                r[3] = 4d;
                r[4] = V128.AllBitsSet;
                r[5] = emptyFunc;
                r[6] = "hello";
            },
                Array.Empty<ValueKind>(),
                new ValueKind[]
                    {
                        ValueKind.Int32,
                        ValueKind.Int64,
                        ValueKind.Float32,
                        ValueKind.Float64,
                        ValueKind.V128,
                        ValueKind.FuncRef,
                        ValueKind.ExternRef
                    }
            ));

            Linker.Define("env", "accept_all_types", Function.FromCallback(Store,
                (int i1, long l2, float f3, double d4, V128 v5, Function f6, object o7) =>
                {
                    (i1, l2, f3, d4, v5, f6.IsNull, o7)
                    .Should().Be((1, 2L, 3f, 4d, V128.AllBitsSet, emptyFunc.IsNull, "hello"));
                }));

            var echoMultipleValuesFunc = EchoMultipleValues;
            Linker.Define("env", "pass_through_multiple_values1", Function.FromCallback(Store, echoMultipleValuesFunc));
            Linker.Define("env", "pass_through_multiple_values2", Function.FromCallback(Store, (long l, double d, object o) => EchoMultipleValues(l, d, o)));

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
        public void WrappedFunctionIsNotCovariant()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var func = instance.GetFunction("return_funcref")!;

            // This function returns `Function`, not `externref`, so this should be null
            func.WrapFunc<object>().Should().BeNull();

            // Wrap it as returning a `Function`, which should return as we expect
            func.WrapFunc<Function>().Should().NotBeNull();

            // Check that the cache does not give us back that function
            func.WrapFunc<object>().Should().BeNull();

            // Check that the cache still works
            var a = func.WrapFunc<Function>();
            var b = func.WrapFunc<Function>();
            a.Should().BeSameAs(b);
        }

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

        [Fact]
        public void ItReturnsAndAcceptsAllTypes()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var action = instance.GetAction("get_and_pass_all_types");
            action.Should().NotBeNull();

            action.Invoke();
        }

        [Fact]
        public void ItAcceptsDefinitionWithTupleReturn()
        {
            Linker.DefineFunction<(int, int)>("", "invalid", () => (1, 2));
        }

        [Fact]
        public void ItReturnsAndAcceptsAllTypesWithUntypedCallbacks()
        {
            Linker.AllowShadowing = true;

            var emptyFunc = Function.FromCallback(Store, () => { });
            bool setResults = true;

            Linker.Define("env", "return_all_types", Function.FromCallback(Store, (Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results) =>
                {
                    arguments.Length.Should().Be(0);
                    results.Length.Should().Be(7);

                    results[0].Kind.Should().Be(ValueKind.Int32);
                    results[1].Kind.Should().Be(ValueKind.Int64);
                    results[2].Kind.Should().Be(ValueKind.Float32);
                    results[3].Kind.Should().Be(ValueKind.Float64);
                    results[4].Kind.Should().Be(ValueKind.V128);
                    results[5].Kind.Should().Be(ValueKind.FuncRef);
                    results[6].Kind.Should().Be(ValueKind.ExternRef);

                    if (setResults)
                    {
                        results[0] = 1;
                        results[1] = 2L;
                        results[2] = 3f;
                        results[3] = 4d;
                        results[4] = V128.AllBitsSet;
                        results[5] = emptyFunc;
                        results[6] = "hello";
                    }
                    else
                    {
                        // Set a result with an incompatible type, which should later
                        // throw an InvalidCastException.
                        results[0] = new ValueBox(null);
                    }
                },
                Array.Empty<ValueKind>(),
                new[] { ValueKind.Int32, ValueKind.Int64, ValueKind.Float32, ValueKind.Float64, ValueKind.V128, ValueKind.FuncRef, ValueKind.ExternRef }));

            Linker.Define("env", "accept_all_types", Function.FromCallback(Store, (Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results) =>
                {
                    arguments.Length.Should().Be(7);
                    results.Length.Should().Be(0);

                    arguments[0].Kind.Should().Be(ValueKind.Int32);
                    arguments[1].Kind.Should().Be(ValueKind.Int64);
                    arguments[2].Kind.Should().Be(ValueKind.Float32);
                    arguments[3].Kind.Should().Be(ValueKind.Float64);
                    arguments[4].Kind.Should().Be(ValueKind.V128);
                    arguments[5].Kind.Should().Be(ValueKind.FuncRef);
                    arguments[6].Kind.Should().Be(ValueKind.ExternRef);

                    var i1 = arguments[0].AsInt32();
                    var l2 = arguments[1].AsInt64();
                    var f3 = arguments[2].AsSingle();
                    var d4 = arguments[3].AsDouble();
                    var v5 = arguments[4].AsV128();
                    var f6 = arguments[5].AsFunction(Store);
                    var o7 = arguments[6].As<string>();

                    (i1, l2, f3, d4, v5, f6.IsNull, o7)
                        .Should().Be((1, 2L, 3f, 4d, V128.AllBitsSet, emptyFunc.IsNull, "hello"));

                    var arg0 = arguments[0];
                    Action shouldThrow = () => arg0.AsFunction(Store);
                    shouldThrow.Should().Throw<InvalidCastException>().WithMessage("Cannot convert from `Int32` to `FuncRef`");

                    var arg6 = arguments[6];
                    shouldThrow = () => arg6.AsInt32();
                    shouldThrow.Should().Throw<InvalidCastException>().WithMessage("Cannot convert from `ExternRef` to `Int32`");
                },
                new[] { ValueKind.Int32, ValueKind.Int64, ValueKind.Float32, ValueKind.Float64, ValueKind.V128, ValueKind.FuncRef, ValueKind.ExternRef },
                Array.Empty<ValueKind>()));

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var action = instance.GetAction("get_and_pass_all_types");
            action.Should().NotBeNull();

            action.Invoke();

            setResults = false;
            action.Should().Throw<WasmtimeException>().WithMessage("*Cannot convert from `ExternRef` to `Int32`*")
                .WithInnerException<InvalidCastException>().WithMessage("Cannot convert from `ExternRef` to `Int32`");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }

        public delegate (long, double, object) EchoMultipleValuesCustomDelegate(long l, double d, object o);
    }
}
