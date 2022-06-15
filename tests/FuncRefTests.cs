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

            Callback = Function.FromCallback(Store, (Caller caller, Function f) => f.Invoke(caller, "testing"));
            Assert = Function.FromCallback(Store, (string s) => { s.Should().Be("testing"); return "asserted!"; });

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
            var f = Function.FromCallback(Store, (Caller caller, string s) => Assert.Invoke(caller, s));
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<Function, Function, string>(Store, "call_nested");
            func.Should().NotBeNull();

            func(Callback, f).Should().Be("asserted!");
        }

        [Fact]
        public void ItAcceptsFunctionReferences()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var func = instance.GetFunction<string>(Store, "call_callback");
            func.Should().NotBeNull();

            func().Should().Be("asserted!");
        }

        [Fact]
        public void ItThrowsForInvokingANullFunctionReference()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var func = instance.GetFunction(Store, "call_with_null");

            Action action = () => func.Invoke(Store);

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
