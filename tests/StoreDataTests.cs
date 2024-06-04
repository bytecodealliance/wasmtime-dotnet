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

        unsafe class RefCounter
        {
            public int Counter { get => *counter; }

            internal RefCounter(int* counter)
            {
                this.counter = counter;
                System.Threading.Interlocked.Increment(ref *counter);
            }

            ~RefCounter()
            {
                System.Threading.Interlocked.Decrement(ref *counter);
            }

            int* counter;
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

        [Fact]
        unsafe public void ItCollectsExistingData()
        {
            var counter = 0;

            RunTest(&counter);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            counter.Should().Be(0);

            void RunTest(int* counter)
            {
                var storeData = new RefCounter(counter);
                var store = new Store(Fixture.Engine, storeData);

                Linker.DefineFunction("", "hello", ((Caller caller) =>
                {
                    var cnt = caller.GetData() as RefCounter;
                    cnt.Counter.Should().Be(1);
                }));

                var instance = Linker.Instantiate(store, Fixture.Module);
                var run = instance.GetFunction("run");

                run.Should().NotBeNull();
                run.Invoke();
                store.Dispose();
            }
        }

        [Fact]
        unsafe public void ItCollectsExistingDataAfterSetData()
        {
            var counter = 0;

            RunTest(&counter);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            counter.Should().Be(0);

            void RunTest(int* counter)
            {
                var storeData = new RefCounter(counter);
                var store = new Store(Fixture.Engine, storeData);

                Linker.DefineFunction("", "hello", ((Caller caller) =>
                {
                    var cnt = caller.GetData() as RefCounter;
                    cnt.Counter.Should().Be(1);

                    caller.SetData(null);
                }));

                var instance = Linker.Instantiate(store, Fixture.Module);
                var run = instance.GetFunction("run");
                run.Should().NotBeNull();
                run.Invoke();

                store.GetData().Should().BeNull();
            }
        }

        public void Dispose()
        {
            Linker.Dispose();
        }
    }
}