using FluentAssertions;
using System.IO;
using Xunit;

namespace Wasmtime.Tests
{
    public class StoreTests
        : StoreFixture
    {
        [Fact]
        public void ItSetsLimits()
        {
            Store.SetLimits(1, 2, 3, 4, 5);
        }

        [Fact]
        public void ItSetsDefaultLimits()
        {
            Store.SetLimits(null, null, null, null, null);
        }

        [Fact]
        public void ItLimitsMemorySize()
        {
            Store.SetLimits(memorySize: Memory.PageSize);

            var memory = new Memory(Store, 0, 1024);
            memory.GetSize().Should().Be(0);


            memory.Grow(1).Should().Be(0);

            var act = () => { memory.Grow(1); };
            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItLimitsTableElements()
        {
            Store.SetLimits(tableElements: 5);

            var table = new Table(Store, TableKind.ExternRef, null, 0);
            table.GetSize().Should().Be(0);

            table.Grow(5, null);

            var act = () => { table.Grow(1, null); };
            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItLimitsInstances()
        {
            Store.SetLimits(instances: 3);

            using var module = Module.FromTextFile(Engine, Path.Combine("Modules", "Trap.wat"));

            var inst1 = new Instance(Store, module);
            var inst2 = new Instance(Store, module);
            var inst3 = new Instance(Store, module);

            var act = () => { new Instance(Store, module); };
            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItLimitsTables()
        {
            Store.SetLimits(tables: 3);

            // This module exports exactly 3 tables
            using var module = Module.FromTextFile(Engine, Path.Combine("Modules", "TableExports.wat"));

            var inst1 = new Instance(Store, module);

            var act = () => { new Instance(Store, module); };
            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItLimitsMemories()
        {
            Store.SetLimits(memories: 2);

            // This module exports 1 memory
            using var module = Module.FromTextFile(Engine, Path.Combine("Modules", "MemoryExports.wat"));

            var inst1 = new Instance(Store, module);
            var inst2 = new Instance(Store, module);

            var act = () => { new Instance(Store, module); };
            act.Should().Throw<WasmtimeException>();
        }
    }
}
