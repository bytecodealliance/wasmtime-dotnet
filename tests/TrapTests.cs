using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TrapFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Trap.wat";
    }

    public class TrapTests : IClassFixture<TrapFixture>, IDisposable
    {
        private TrapFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public TrapTests(TrapFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItIncludesAStackTrace()
        {
            Action action = () =>
            {
                var instance = Linker.Instantiate(Store, Fixture.Module);
                var run = instance.GetAction("run");
                run.Should().NotBeNull();
                run();
            };

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Frames.Count == 4 &&
                            e.Frames[0].FunctionName == "third" &&
                            e.Frames[1].FunctionName == "second" &&
                            e.Frames[2].FunctionName == "first" &&
                            e.Frames[3].FunctionName == "run")
                .WithMessage("wasm trap: wasm `unreachable` instruction executed*");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
