using System;
using System.Collections.Generic;
using System.Linq;
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
            Store = new Store(Fixture.Engine);
            Host = new Host(Store);

            Callback = Host.DefineFunction("", "callback", (Function f) => f.Invoke("testing"));
            Assert = Host.DefineFunction("", "assert", (string s) => { s.Should().Be("testing"); return "asserted!"; });
        }

        private FuncRefFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Host Host { get; set; }

        private Function Callback { get; set; }

        private Function Assert { get; set; }

        [Fact]
        public void ItPassesFunctionReferencesToWasm()
        {
            using var func = Function.FromCallback(Store, (string s) => Assert.Invoke(s));
            using dynamic instance = Host.Instantiate(Fixture.Module);

            (instance.call_nested(Callback, func) as string).Should().Be("asserted!");
        }

        [Fact]
        public void ItAcceptsFunctionReferences()
        {
            using dynamic instance = Host.Instantiate(Fixture.Module);

            (instance.call_callback() as string).Should().Be("asserted!");
        }

        [Fact]
        public void ItThrowsForInvokingANullFunctionReference()
        {
            using dynamic instance = Host.Instantiate(Fixture.Module);

            Action action = () => instance.call_with_null();

            action
                .Should()
                .Throw<TrapException>()
                .WithMessage("Cannot invoke a null function reference.");
        }

        public void Dispose()
        {
            Store.Dispose();
            Host.Dispose();
            Callback.Dispose();
            Assert.Dispose();
        }
    }
}
