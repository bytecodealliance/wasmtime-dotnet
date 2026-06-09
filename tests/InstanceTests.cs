using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public sealed class InstanceFixture
        : ModuleFixture
    {
        protected override string ModuleFileName => "hello.wat";
    }

    public sealed class InstanceTests
        : IClassFixture<InstanceFixture>, IDisposable
    {
        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public InstanceTests(InstanceFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Linker.DefineFunction("env", "add", (int x, int y) => x + y);
            Linker.DefineFunction("env", "swap", (int x, int y) => (y, x));
            Linker.DefineFunction("", "hi", (int x, int y) => (y, x));

            Linker.DefineFunction("", "hello", () => { });
        }

        private InstanceFixture Fixture { get; }

        [Fact]
        public void ItGetsExportedFunctions()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var results = instance.GetFunctions();

            results.Single().Name.Should().Be("run");
        }

        [Fact]
        public void ItThrowsWithNullStore()
        {
            Assert.Throws<ArgumentNullException>(() => new Instance(null!, Fixture.Module));
        }

        [Fact]
        public void ItThrowsWithNullModule()
        {
            Assert.Throws<ArgumentNullException>(() => new Instance(Store, null!));
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
