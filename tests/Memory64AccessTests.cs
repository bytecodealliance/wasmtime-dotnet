using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Wasmtime.Tests
{
    public class Memory64AccessFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Memory64Access.wat";
    }

    public class Memory64AccessTests : IClassFixture<Memory64AccessFixture>, IDisposable
    {
        private Memory64AccessFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public Memory64AccessTests(Memory64AccessFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact(Skip = "Test consumes too much memory for CI")]
        public unsafe void ItCanAccessMemoryWith65537Pages()
        {
            var memoryExport = Fixture.Module.Exports.OfType<MemoryExport>().Single();
            memoryExport.Minimum.Should().Be(0x10001);
            memoryExport.Maximum.Should().Be(0x1000000000000);
            memoryExport.Is64Bit.Should().BeTrue();

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem")!.Value;

            memory.Minimum.Should().Be(0x10001);
            memory.Maximum.Should().Be(0x1000000000000);
            memory.Is64Bit.Should().BeTrue();
            memory.GetSize().Should().Be(0x10001);
            memory.GetLength().Should().Be(0x100010000);

            memory.ReadInt32(0).Should().Be(0);

            memory.WriteInt64(100, 1234);
            memory.ReadInt64(100).Should().Be(1234);

            memory.ReadByte(0x10000FFFF).Should().Be(0x63);
            memory.ReadInt16(0x10000FFFE).Should().Be(0x6364);
            memory.ReadInt32(0x10000FFFC).Should().Be(0x63646500);

            memory.ReadSingle(0x10000FFFC).Should().Be(4.2131355E+21F);

            var span = memory.GetSpan(0x10000FFFE, 2);
            span.SequenceEqual(new byte[] { 0x64, 0x63 }).Should().BeTrue();

            var int16Span = memory.GetSpan<short>(0x10000, int.MaxValue);
            int16Span[0x7FFFFFFE].Should().Be(0x6500);

            int16Span = memory.GetSpan<short>(0x10002);
            int16Span[0x7FFFFFFE].Should().Be(0x6364);

            byte* ptr = (byte*)memory.GetPointer();
            ptr += 0x10000FFFF;
            (*ptr).Should().Be(0x63);

            string str1 = "Hello World";
            memory.WriteString(0x10000FFFF - str1.Length, str1);
            memory.ReadString(0x10000FFFF - str1.Length, str1.Length).Should().Be(str1);
        }

        [Fact(Skip = "Test consumes too much memory for CI")]
        public void ItThrowsForOutOfBoundsAccess()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem")!.Value;

#pragma warning disable CS0618 // Type or member is obsolete
            Action action = () => memory.GetSpan();
#pragma warning restore CS0618 // Type or member is obsolete
            action.Should().Throw<OverflowException>();

            action = () => memory.GetSpan<short>(0x10000);
            action.Should().Throw<OverflowException>();

            action = () => memory.GetSpan(-1L, 0);
            action.Should().Throw<ArgumentOutOfRangeException>();

            action = () => memory.GetSpan(0L, -1);
            action.Should().Throw<ArgumentOutOfRangeException>();

            action = () => memory.ReadInt16(0x10000FFFF);
            action.Should().Throw<ArgumentException>();

            action = () => memory.GetSpan<short>(0x10000FFFF, 1);
            action.Should().Throw<ArgumentException>();
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
