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
        private StoreDataFixture Fixture { get; }

        private Linker Linker { get; set; }

        public StoreDataTests(StoreDataFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItAddsDataToTheStore()
        {
            var msg = "Hello!";
            using var store = new Store(Fixture.Engine, msg);

            Linker.DefineFunction("", "hello", ((Caller caller) =>
            {
                var data = caller.GetData() as string;
                data.Should().NotBeNull();
                data.Should().Be(msg);
            }));

            var instance = Linker.Instantiate(store, Fixture.Module);
            var func = instance.GetFunction("run");
            func.Should().NotBeNull();

            func.Invoke();
        }

        [Fact]
        public void ItReturnsNullWhenNoDataWasInitialized()
        {
            using var store = new Store(Fixture.Engine);

            Linker.DefineFunction("", "hello", ((Caller caller) =>
            {
                var data = caller.GetData();
                data.Should().BeNull();
            }));
            
            var instance = Linker.Instantiate(store, Fixture.Module);
            var func = instance.GetFunction("run");
            func.Should().NotBeNull();

            func.Invoke();
        }

        [Fact]
        public void ItShouldReplaceStoreData()
        {
            var msg = "Hello!";
            using var store = new Store(Fixture.Engine, msg);

            Linker.DefineFunction("", "hello", ((Caller caller) =>
            {
                caller.SetData(new int[] { 1, 2, 3 });
            }));
            
            var instance = Linker.Instantiate(store, Fixture.Module);
            var func = instance.GetFunction("run");
            func.Should().NotBeNull();

            func.Invoke();

            var data = store.GetData() as int[];
            data.Should().NotBeNull();
            data.Should().BeOfType<int[]>();
        }

        public void Dispose()
        {
            Linker.Dispose();
        }
    }
}