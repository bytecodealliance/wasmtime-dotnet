using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryExportsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryExports.wat";
    }

    public class MemoryExportsTests : IClassFixture<MemoryExportsFixture>, IDisposable
    {
        private MemoryExportsFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public MemoryExportsTests(MemoryExportsFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Theory]
        [MemberData(nameof(GetMemoryExports))]
        public void ItHasTheExpectedMemoryExports(string exportName, uint expectedMinimum, uint expectedMaximum)
        {
            var export = Fixture.Module.Exports.Where(m => m.Name == exportName).FirstOrDefault() as MemoryExport;
            export.Should().NotBeNull();
            export.Minimum.Should().Be(expectedMinimum);
            export.Maximum.Should().Be(expectedMaximum);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedTables()
        {
            GetMemoryExports().Count().Should().Be(Fixture.Module.Exports.Count(e => e is MemoryExport));
        }

        [Fact]
        public void ItReadsAndWritesGenericTypes()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem");

            memory.Should().NotBeNull();

            memory.Write(11, new TestStruct { A = 17, B = -34346 });
            var result = memory.Read<TestStruct>(11);

            result.A.Should().Be(17);
            result.B.Should().Be(-34346);
        }

        struct TestStruct
        {
            public byte A;
            public int B;
        }

        [Fact]
        public void ItCreatesExternsForTheMemories()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var memory = instance.GetMemory("mem");

            memory.Should().NotBeNull();

            memory.ReadString(0, 11).Should().Be("Hello World");
            int written = memory.WriteString(0, "WebAssembly Rocks!");
            memory.ReadString(0, written).Should().Be("WebAssembly Rocks!");

            memory.ReadByte(20).Should().Be(1);
            memory.WriteByte(20, 11);
            memory.ReadByte(20).Should().Be(11);

            memory.ReadInt16(21).Should().Be(2);
            memory.WriteInt16(21, 12);
            memory.ReadInt16(21).Should().Be(12);

            memory.ReadInt32(23).Should().Be(3);
            memory.WriteInt32(23, 13);
            memory.ReadInt32(23).Should().Be(13);

            memory.ReadInt64(27).Should().Be(4);
            memory.WriteInt64(27, 14);
            memory.ReadInt64(27).Should().Be(14);

            memory.ReadSingle(35).Should().Be(5);
            memory.WriteSingle(35, 15);
            memory.ReadSingle(35).Should().Be(15);

            memory.ReadDouble(39).Should().Be(6);
            memory.WriteDouble(39, 16);
            memory.ReadDouble(39).Should().Be(16);

            memory.ReadIntPtr(48).Should().Be((IntPtr)7);
            memory.WriteIntPtr(48, (IntPtr)17);
            memory.ReadIntPtr(48).Should().Be((IntPtr)17);
        }

        public static IEnumerable<object[]> GetMemoryExports()
        {
            yield return new object[] {
                "mem",
                1,
                2
            };
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
