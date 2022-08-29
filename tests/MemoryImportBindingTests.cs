using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryImportBindingFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryImportBinding.wat";
    }

    public class MemoryImportBindingTests : IClassFixture<MemoryImportBindingFixture>, IDisposable
    {
        private MemoryImportBindingFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public MemoryImportBindingTests(MemoryImportBindingFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItFailsToInstantiateWithMissingImport()
        {
            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("unknown import: `::mem` has not been defined");
        }

        [Fact]
        public void ItBindsTheGlobalsCorrectly()
        {
            var mem = new Memory(Store, 1);
            Linker.Define("", "mem", mem);
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var readByte = instance.GetFunction("ReadByte");
            var readInt16 = instance.GetFunction("ReadInt16");
            var readInt32 = instance.GetFunction("ReadInt32");
            var readInt64 = instance.GetFunction("ReadInt64");
            var readFloat32 = instance.GetFunction("ReadFloat32");
            var readFloat64 = instance.GetFunction("ReadFloat64");
            var readIntPtr = instance.GetFunction("ReadIntPtr");

            mem.ReadString(0, 11).Should().Be("Hello World");
            int written = mem.WriteString(0, "WebAssembly Rocks!");
            mem.ReadString(0, written).Should().Be("WebAssembly Rocks!");

            mem.ReadByte(20).Should().Be(1);
            mem.WriteByte(20, 11);
            mem.ReadByte(20).Should().Be(11);
            readByte.Invoke().Should().Be(11);

            mem.ReadInt16(21).Should().Be(2);
            mem.WriteInt16(21, 12);
            mem.ReadInt16(21).Should().Be(12);
            readInt16.Invoke().Should().Be(12);

            mem.ReadInt32(23).Should().Be(3);
            mem.WriteInt32(23, 13);
            mem.ReadInt32(23).Should().Be(13);
            readInt32.Invoke().Should().Be(13);

            mem.ReadInt64(27).Should().Be(4);
            mem.WriteInt64(27, 14);
            mem.ReadInt64(27).Should().Be(14);
            readInt64.Invoke().Should().Be(14);

            mem.ReadSingle(35).Should().Be(5);
            mem.WriteSingle(35, 15);
            mem.ReadSingle(35).Should().Be(15);
            readFloat32.Invoke().Should().Be(15);

            mem.ReadDouble(39).Should().Be(6);
            mem.WriteDouble(39, 16);
            mem.ReadDouble(39).Should().Be(16);
            readFloat64.Invoke().Should().Be(16);

            mem.ReadIntPtr(48).Should().Be((IntPtr)7);
            mem.WriteIntPtr(48, (IntPtr)17);
            mem.ReadIntPtr(48).Should().Be((IntPtr)17);
            readIntPtr.Invoke().Should().Be(17);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
