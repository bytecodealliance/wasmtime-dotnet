using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryAccessFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryAccess.wat";
    }

    public class MemoryAccessTests : IClassFixture<MemoryAccessFixture>, IDisposable
    {
        private MemoryAccessFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public MemoryAccessTests(MemoryAccessFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public unsafe void ItCanAccessMemoryWith65536Pages()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem");

            memory.GetLength().Should().Be(uint.MaxValue + 1L);

            memory.ReadInt32(0).Should().Be(0);
            memory.ReadInt32(0L).Should().Be(0);

            memory.WriteInt64(100, 1234);
            memory.ReadInt64(100L).Should().Be(1234);

            memory.ReadByte(uint.MaxValue).Should().Be(0x63);
            memory.ReadInt16(uint.MaxValue - 1).Should().Be(0x6364);
            memory.ReadInt32(uint.MaxValue - 3).Should().Be(0x63646500);

            memory.ReadSingle(uint.MaxValue - 3).Should().Be(4.2131355E+21F);

            var span = memory.GetSpan(uint.MaxValue - 1, 2);
            span.SequenceEqual(new byte[] { 0x64, 0x63 }).Should().BeTrue();

            var int16Span = memory.GetSpan<short>(0, int.MaxValue);
            int16Span[int.MaxValue - 1].Should().Be(0x6500);

            int16Span = memory.GetSpan<short>(2);
            int16Span[int.MaxValue - 1].Should().Be(0x6364);

            byte* ptr = (byte*)memory.GetPointer();
            ptr += uint.MaxValue;
            (*ptr).Should().Be(0x63);

            string str1 = "Hello World";
            memory.WriteString(uint.MaxValue - str1.Length, str1);
            memory.ReadString(uint.MaxValue - str1.Length, str1.Length).Should().Be(str1);
        }

        [Fact]
        public void ItThrowsForOutOfBoundsAccess()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem");

#pragma warning disable CS0618 // Type or member is obsolete
            Action action = () => memory.GetSpan();
#pragma warning restore CS0618 // Type or member is obsolete
            action.Should().Throw<OverflowException>();

            action = () => memory.GetSpan<short>(0);
            action.Should().Throw<OverflowException>();

            action = () => memory.GetSpan(-1L, 0);
            action.Should().Throw<ArgumentOutOfRangeException>();

            action = () => memory.GetSpan(0L, -1);
            action.Should().Throw<ArgumentOutOfRangeException>();

            action = () => memory.ReadInt16(uint.MaxValue);
            action.Should().Throw<ArgumentException>();

            action = () => memory.GetSpan<short>(uint.MaxValue, 1);
            action.Should().Throw<ArgumentException>();
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
