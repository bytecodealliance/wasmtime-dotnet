using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class StoreDataFixture : ModuleFixture
    {
        protected override string ModuleFileName => "hello.wat";
    }

    public class StoreDataTests : IClassFixture<StoreDataFixture>, IDisposable
    {
        private class StoreData
        {
            public object Value { get; set; }

            public StoreData(object value)
            {
                Value = value;
            }
        }

        private StoreDataFixture Fixture { get; }

        private Linker Linker { get; set; }

        private Store Store { get; set; }

        public StoreDataTests(StoreDataFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItAddsDataToTheStore()
        {
            var msg = "Hello!";
            var data = new StoreData(msg);
            using var store = new Store(Fixture.Engine, data);

            Linker.DefineFunction("", "hello", ((Caller caller) =>
            {
                var data = caller.GetData() as StoreData;
                data.Value.Should().Be(msg);
            }));
        }

        [Fact]
        public void ItReturnsNullWhenNoDataWasInitialized()
        {
            var msg = "Hello!";
            var data = new StoreData(msg);
            using var store = new Store(Fixture.Engine);

            Linker.DefineFunction("", "hello", ((Caller caller) =>
            {
                var data = caller.GetData();
                data.IsNull.Should().BeTrue();
            }));
        }

        public void Dispose()
        {
            Linker.Dispose();
        }
    }
}