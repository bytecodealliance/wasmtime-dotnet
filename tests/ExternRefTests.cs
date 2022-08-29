using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ExternRefFixture : ModuleFixture
    {
        protected override string ModuleFileName => "ExternRef.wat";
    }

    public class ExternRefTests : IClassFixture<ExternRefFixture>, IDisposable
    {
        public ExternRefTests(ExternRefFixture fixture)
        {
            Fixture = fixture;
            Linker = new Linker(Fixture.Engine);
            Store = new Store(Fixture.Engine);

            Linker.Define("", "inout", Function.FromCallback(Store, (object o) => o));
        }

        private ExternRefFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        [Fact]
        public void ItReturnsTheSameDotnetReference()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var inout = instance.GetFunction<string, string>("inout");
            inout.Should().NotBeNull();

            var input = "input";
            inout(input).Should().BeSameAs(input);
        }

        [Fact]
        public void ItHandlesNullReferences()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var inout = instance.GetFunction("inout");
            inout.Should().NotBeNull();

            var nullref = instance.GetFunction("nullref");
            inout.Should().NotBeNull();

            (inout.Invoke(ValueBox.AsBox((object)null))).Should().BeNull();
            (nullref.Invoke()).Should().BeNull();
        }

        unsafe class Value
        {
            internal Value(int* counter)
            {
                this.counter = counter;

                System.Threading.Interlocked.Increment(ref *counter);
            }

            ~Value()
            {
                System.Threading.Interlocked.Decrement(ref *counter);
            }

            int* counter;
        }

        [Fact]
        unsafe public void ItCollectsExternRefs()
        {
            var counter = 0;

            RunTest(&counter);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            counter.Should().Be(0);

            void RunTest(int* counter)
            {
                var instance = Linker.Instantiate(Store, Fixture.Module);

                var inout = instance.GetFunction("inout");
                inout.Should().NotBeNull();
                for (int i = 0; i < 100; ++i)
                {
                    inout.Invoke(ValueBox.AsBox(new Value(counter)));
                }

                Store.Dispose();
                Store = null;
            }
        }

        [Fact]
        public void ItThrowsForMismatchedTypes()
        {
            Linker.AllowShadowing = true;
            Linker.Define("", "inout", Function.FromCallback(Store, (string o) => o));

            var instance = Linker.Instantiate(Store, Fixture.Module);

            var inout = instance.GetFunction("inout");
            inout.Should().NotBeNull();

            Action action = () => inout.Invoke(ValueBox.AsBox((object)5));

            action
                .Should()
                .Throw<Wasmtime.TrapException>()
                .WithMessage("Object of type 'System.Int32' cannot be converted to type 'System.String'*");
        }

        public void Dispose()
        {
            if (Store != null)
            {
                Store.Dispose();
            }
            Linker.Dispose();
        }
    }
}
