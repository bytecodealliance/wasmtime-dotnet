using System;
using System.Collections.Generic;
using System.Linq;
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
            Host = new Host(Fixture.Engine);

            Host.DefineFunction("", "inout", (object o) => o);
        }

        private ExternRefFixture Fixture { get; set; }

        private Host Host { get; set; }

        [Fact]
        public void ItReturnsTheSameDotnetReference()
        {
            using dynamic instance = Host.Instantiate(Fixture.Module);

            var input = "input";
            (instance.inout(input) as string).Should().BeSameAs(input);
        }

        [Fact]
        public void ItHandlesNullReferences()
        {
            using dynamic instance = Host.Instantiate(Fixture.Module);

            (instance.inout(null) as object).Should().BeNull();
            (instance.nullref() as object).Should().BeNull();
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
                // Use a separate host that can be disposed within this test
                // TODO: for > 0.19.0, trigger a Wasmtime GC manually on the same host
                using var host = new Host(Fixture.Engine);
                using var function = host.DefineFunction("", "inout", (object o) => o);
                using dynamic instance = host.Instantiate(Fixture.Module);

                for (int i = 0; i < 100; ++i)
                {
                    instance.inout(new Value(counter));
                }
            }
        }

        [Fact]
        public void ItThrowsForMismatchedTypes()
        {
            using var host = new Host(Fixture.Engine);
            using var function = host.DefineFunction("", "inout", (string o) => o);
            using dynamic instance = host.Instantiate(Fixture.Module);

            Action action = () => instance.inout((object)5);

            action.Should().Throw<Wasmtime.TrapException>().WithMessage("Object of type 'System.Int32' cannot be converted to type 'System.String'*");
        }

        public void Dispose()
        {
            Host.Dispose();
        }
    }
}
