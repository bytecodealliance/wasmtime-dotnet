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
            Action action = () => { Linker.Instantiate(Store.Context, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("unknown import: `::mem` has not been defined");
        }

        [Fact]
        public void ItBindsTheGlobalsCorrectly()
        {
            var context = Store.Context;
            var mem = new Memory(context, 1);
            Linker.Define("", "mem", mem);
            var instance = Linker.Instantiate(context, Fixture.Module);
            var readByte = instance.GetFunction(context, "ReadByte");
            var readInt16 = instance.GetFunction(context, "ReadInt16");
            var readInt32 = instance.GetFunction(context, "ReadInt32");
            var readInt64 = instance.GetFunction(context, "ReadInt64");
            var readFloat32 = instance.GetFunction(context, "ReadFloat32");
            var readFloat64 = instance.GetFunction(context, "ReadFloat64");
            var readIntPtr = instance.GetFunction(context, "ReadIntPtr");

            mem.ReadString(context, 0, 11).Should().Be("Hello World");
            int written = mem.WriteString(context, 0, "WebAssembly Rocks!");
            mem.ReadString(context, 0, written).Should().Be("WebAssembly Rocks!");

            mem.ReadByte(context, 20).Should().Be(1);
            mem.WriteByte(context, 20, 11);
            mem.ReadByte(context, 20).Should().Be(11);
            readByte.Invoke(context).Should().Be(11);

            mem.ReadInt16(context, 21).Should().Be(2);
            mem.WriteInt16(context, 21, 12);
            mem.ReadInt16(context, 21).Should().Be(12);
            readInt16.Invoke(context).Should().Be(12);

            mem.ReadInt32(context, 23).Should().Be(3);
            mem.WriteInt32(context, 23, 13);
            mem.ReadInt32(context, 23).Should().Be(13);
            readInt32.Invoke(context).Should().Be(13);

            mem.ReadInt64(context, 27).Should().Be(4);
            mem.WriteInt64(context, 27, 14);
            mem.ReadInt64(context, 27).Should().Be(14);
            readInt64.Invoke(context).Should().Be(14);

            mem.ReadSingle(context, 35).Should().Be(5);
            mem.WriteSingle(context, 35, 15);
            mem.ReadSingle(context, 35).Should().Be(15);
            readFloat32.Invoke(context).Should().Be(15);

            mem.ReadDouble(context, 39).Should().Be(6);
            mem.WriteDouble(context, 39, 16);
            mem.ReadDouble(context, 39).Should().Be(16);
            readFloat64.Invoke(context).Should().Be(16);

            mem.ReadIntPtr(context, 48).Should().Be((IntPtr)7);
            mem.WriteIntPtr(context, 48, (IntPtr)17);
            mem.ReadIntPtr(context, 48).Should().Be((IntPtr)17);
            readIntPtr.Invoke(context).Should().Be(17);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
