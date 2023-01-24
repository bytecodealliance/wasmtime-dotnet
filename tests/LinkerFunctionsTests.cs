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
            Linker.DefineFunction("env", "add_reflection", (Delegate)((int x, int y) => x + y));
            Linker.DefineFunction("env", "swap", (int x, int y) => (y, x));
            Linker.DefineFunction("env", "do_throw", () => throw new Exception(THROW_MESSAGE));
            Linker.DefineFunction("env", "check_string", (Caller caller, int address, int length) =>
            {
                caller.GetMemory("mem").ReadString(address, length).Should().Be("Hello World");
            });

            Linker.DefineFunction("env", "return_i32", GetBoundFuncIntDelegate());

            Linker.DefineFunction("env", "return_15_values", () =>
            {
                return (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
            });

            Linker.DefineFunction("env", "accept_15_values",
                (int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12, int i13, int i14, int i15) =>
                {
                    (i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14, i15)
                        .Should().Be((1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15));
                });

            var emptyFunc = Function.FromCallback(Store, () => { });
            Linker.DefineFunction("env", "return_all_types", () =>
            {
                return (1, 2L, 3f, 4d, V128.AllBitsSet, emptyFunc, "hello");
            });

            Linker.DefineFunction("env", "accept_all_types", 
                (int i1, long l2, float f3, double d4, V128 v5, Function f6, object o7) =>
                {
                    (i1, l2, f3, d4, v5, f6.IsNull, o7)
                    .Should().Be((1, 2L, 3f, 4d, V128.AllBitsSet, emptyFunc.IsNull, "hello"));
                });

            var echoMultipleValuesFunc = EchoMultipleValues;
            Linker.DefineFunction("env", "pass_through_multiple_values1", echoMultipleValuesFunc);
            Linker.DefineFunction("env", "pass_through_multiple_values2", (FunctionTests.EchoMultipleValuesCustomDelegate)EchoMultipleValues);

            Linker.DefineFunction("env", "pass_through_v128", (V128 v128) => v128);

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

        private LinkerFunctionsFixture Fixture { get; }

        [Fact]
        public void ItBindsImportMethodsAndCallsThemCorrectly()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var add = instance.GetFunction("add");
            var addReflection = instance.GetFunction("add_reflection");
            var swap = instance.GetFunction("swap");
            var check = instance.GetFunction("check_string"); ;
            var getInt32 = instance.GetFunction<int>("return_i32");

            int x = (int)add.Invoke(40, 2);
            x.Should().Be(42);
            x = (int)add.Invoke(22, 5);
            x.Should().Be(27);

            x = (int)addReflection.Invoke(40, 2);
            x.Should().Be(42);
            x = (int)addReflection.Invoke(22, 5);
            x.Should().Be(27);

            x = getInt32.Invoke();
            x.Should().Be(3);

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
        public void ItEchoesMultipleValuesFromFuncDelegate()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var passThrough = instance.GetFunction<long, double, object, (long, double, object)>("pass_through_multiple_values1");
            passThrough.Should().NotBeNull();

            var result = passThrough(1, 2, "3");
            result.Should().Be((1, 2, "3"));
        }

        [Fact]
        public void ItEchoesMultipleValuesFromCustomDelegate()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var passThrough = instance.GetFunction<long, double, object, (long, double, object)>("pass_through_multiple_values2");
            passThrough.Should().NotBeNull();

            var result = passThrough(1, 2, "3");
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
        public void ItFetchesDefinedFunctions()
        {
            var func = Linker.GetFunction(Store, "env", "add")!;

            func.Should().NotBeNull();
            func.Parameters.Should().HaveCount(2);
            func.Results.Should().HaveCount(1);
        }

        [Fact]
        public void ItReturnsNullForUndefinedFunctions()
        {
            var func = Linker.GetFunction(Store, "nosuch", "function")!;

            func.Should().BeNull();
        }

        [Fact]
        public void ItBindsComplexFunction()
        {
            using var store = new Store(Fixture.Engine);

            Linker.DefineFunction("", "complex", ((Caller caller) =>
            {
                return (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19);
            }));

            var func = Linker.GetFunction(store, "", "complex");
            func.Should().NotBeNull();
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
        public void ItReturnsAndAcceptsAllTypesWithUntypedCallbacks()
        {
            Linker.AllowShadowing = true;

            var emptyFunc = Function.FromCallback(Store, () => { });
            bool setResults = true;

            Linker.DefineFunction("env", "return_all_types", (Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results) =>
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
                new[] { ValueKind.Int32, ValueKind.Int64, ValueKind.Float32, ValueKind.Float64, ValueKind.V128, ValueKind.FuncRef, ValueKind.ExternRef });

            Linker.DefineFunction("env", "accept_all_types", (Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results) =>
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
                Array.Empty<ValueKind>());

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
    }
}
