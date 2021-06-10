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

            Callback = Function.FromCallback(Store.Context, (Caller caller, Function f) => f.Invoke(caller.Context, "testing"));
            Assert = Function.FromCallback(Store.Context, (string s) => { s.Should().Be("testing"); return "asserted!"; });

            Linker.Define("", "callback", Callback);
            Linker.Define("", "assert", Assert);
        }

        private FuncRefFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        private Function Callback { get; set; }

        private Function Assert { get; set; }

        [Fact]
        public void ItPassesFunctionReferencesToWasm()
        {
            var context = Store.Context;
            var f = Function.FromCallback(context, (Caller caller, string s) => Assert.Invoke(caller.Context, s));
            var instance = Linker.Instantiate(context, Fixture.Module);
            var func = instance.GetFunction(context, "call_nested");

            (func.Invoke(context, Callback, f) as string).Should().Be("asserted!");
        }

        [Fact]
        public void ItAcceptsFunctionReferences()
        {
            var context = Store.Context;
            var instance = Linker.Instantiate(context, Fixture.Module);
            var func = instance.GetFunction(context, "call_callback");

            (func.Invoke(context) as string).Should().Be("asserted!");
        }

        [Fact]
        public void ItThrowsForInvokingANullFunctionReference()
        {
            var context = Store.Context;
            var instance = Linker.Instantiate(context, Fixture.Module);
            var func = instance.GetFunction(context, "call_with_null");

            Action action = () => func.Invoke(Store.Context);

            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("Cannot invoke a null function reference.*");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
