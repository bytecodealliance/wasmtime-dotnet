using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class FuncRefFixture : ModuleFixture
    {
        protected override string ModuleFileName => "FuncRef.wat";
    }

    public class FuncRefTests : IClassFixture<FuncRefFixture>, IDisposable
    {
        public FuncRefTests(FuncRefFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Callback = Function.FromCallback(Store, (Caller caller, Function f) => f.Invoke("testing"));
            Assert = Function.FromCallback(Store, (string s) => { s.Should().Be("testing"); return "asserted!"; });

            Linker.DefineFunction("", "return_funcref", () => ReturnFuncRefCallback());
            Linker.DefineFunction("", "store_funcref", (Function funcRef) => StoreFuncRefCallback(funcRef));

            Linker.Define("", "callback", Callback);
            Linker.Define("", "assert", Assert);
        }

        private FuncRefFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        private Function Callback { get; set; }

        private Function Assert { get; set; }

        private Func<Function> ReturnFuncRefCallback { get; set; }

        private Action<Function> StoreFuncRefCallback { get; set; }

        [Fact]
        public void ItPassesFunctionReferencesToWasm()
        {
            var f = Function.FromCallback(Store, (Caller caller, string s) => Assert.Invoke(s));
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<Function, Function, string>("call_nested");
            func.Should().NotBeNull();

            func(Callback, f).Should().Be("asserted!");
        }

        [Fact]
        public void ItAcceptsFunctionReferences()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<string>("call_callback");
            func.Should().NotBeNull();

            func().Should().Be("asserted!");
        }

        [Fact]
        public void ItReturnsFunctionReferences()
        {
            ReturnFuncRefCallback = () => Assert;
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<Function>("return_funcref");
            func.Should().NotBeNull();

            var returnedFunc = func();
            returnedFunc.Should().NotBeNull();

            // We can't check whether the returnedFunc is the same as the Assert function
            // (as they will be different instances).
            var wrappedFunc = returnedFunc.WrapFunc<string, string>();
            wrappedFunc.Should().NotBeNull();

            wrappedFunc("testing").Should().Be("asserted!");
        }

        [Fact]
        public void ItReturnsNullFunctionReferences()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<Function>("return_funcref");
            func.Should().NotBeNull();

            ReturnFuncRefCallback = () => Function.Null;
            var returnedFunc = func();
            returnedFunc.IsNull.Should().BeTrue();

            ReturnFuncRefCallback = () => null;
            returnedFunc = func();
            returnedFunc.IsNull.Should().BeTrue();
        }

        [Fact]
        public void ItThrowsWhenReturningFunctionReferencesFromDifferentStore()
        {
            using var separateStore = new Store(Fixture.Engine);
            var separateStoreFunction = Function.FromCallback(separateStore, () => 123);

            ReturnFuncRefCallback = () => separateStoreFunction;
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<Function>("return_funcref");
            func.Should().NotBeNull();

            func
                .Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException is InvalidOperationException)
                .WithMessage("*Returning a Function is only allowed when it belongs to the current store.*");
        }

        [Fact]
        public void ItThrowsForInvokingANullFunctionReference()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var func = instance.GetFunction<object>("call_with_null");
            func.Should().NotBeNull();

            func
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("*Cannot invoke a null function reference.*");
        }

        [Fact]
        public void ItCanUseFunctionReferenceFromCallbackAfterReturning()
        {
            var localFuncRef = default(Function);
            StoreFuncRefCallback = funcRef => localFuncRef = funcRef;

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var func = instance.GetAction("call_store_funcref");
            func.Should().NotBeNull();

            func();

            var wrappedFunc = localFuncRef.WrapFunc<string, string>();

            wrappedFunc
                .Should()
                .NotBeNull();

            wrappedFunc("testing").Should().Be("asserted!");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
