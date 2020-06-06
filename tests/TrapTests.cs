using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TrapFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Trap.wat";
    }

    public class TrapTests : IClassFixture<TrapFixture>
    {
        public TrapTests(TrapFixture fixture)
        {
            Fixture = fixture;

            Fixture.Host.ClearDefinitions();
        }

        private TrapFixture Fixture { get; set; }

        [Fact]
        public void ItIncludesAStackTrace()
        {
            Action action = () =>
            {
                using dynamic instance = Fixture.Host.Instantiate(Fixture.Module);
                instance.run();
            };

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Frames.Count == 4 &&
                            e.Frames[0].FunctionName == "third" &&
                            e.Frames[1].FunctionName == "second" &&
                            e.Frames[2].FunctionName == "first" &&
                            e.Frames[3].FunctionName == "run")
                .WithMessage("wasm trap: unreachable*");
        }
    }
}
