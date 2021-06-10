using System;
using System.Collections.Generic;
using System.Linq;
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
        public void ItCreatesExternsForTheMemories()
        {
            var context = Store.Context;
            var instance = Linker.Instantiate(context, Fixture.Module);
            var memory = instance.GetMemory(context, "mem");

            memory.Should().NotBeNull();

            memory.ReadString(context, 0, 11).Should().Be("Hello World");
            int written = memory.WriteString(context, 0, "WebAssembly Rocks!");
            memory.ReadString(context, 0, written).Should().Be("WebAssembly Rocks!");

            memory.ReadByte(context, 20).Should().Be(1);
            memory.WriteByte(context, 20, 11);
            memory.ReadByte(context, 20).Should().Be(11);

            memory.ReadInt16(context, 21).Should().Be(2);
            memory.WriteInt16(context, 21, 12);
            memory.ReadInt16(context, 21).Should().Be(12);

            memory.ReadInt32(context, 23).Should().Be(3);
            memory.WriteInt32(context, 23, 13);
            memory.ReadInt32(context, 23).Should().Be(13);

            memory.ReadInt64(context, 27).Should().Be(4);
            memory.WriteInt64(context, 27, 14);
            memory.ReadInt64(context, 27).Should().Be(14);

            memory.ReadSingle(context, 35).Should().Be(5);
            memory.WriteSingle(context, 35, 15);
            memory.ReadSingle(context, 35).Should().Be(15);

            memory.ReadDouble(context, 39).Should().Be(6);
            memory.WriteDouble(context, 39, 16);
            memory.ReadDouble(context, 39).Should().Be(16);

            memory.ReadIntPtr(context, 48).Should().Be((IntPtr)7);
            memory.WriteIntPtr(context, 48, (IntPtr)17);
            memory.ReadIntPtr(context, 48).Should().Be((IntPtr)17);
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
