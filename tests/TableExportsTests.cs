using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TableExportsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "TableExports.wat";
    }

    public class TableExportsTests : IClassFixture<TableExportsFixture>, IDisposable
    {
        private TableExportsFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public TableExportsTests(TableExportsFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Theory]
        [MemberData(nameof(GetTableExports))]
        public void ItHasTheExpectedTableExports(string exportName, ValueKind expectedKind, uint expectedMinimum, uint expectedMaximum)
        {
            var export = Fixture.Module.Exports.Where(f => f.Name == exportName).FirstOrDefault() as TableExport;
            export.Should().NotBeNull();
            export.Kind.Should().Be(expectedKind);
            export.Minimum.Should().Be(expectedMinimum);
            export.Maximum.Should().Be(expectedMaximum);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedTables()
        {
            GetTableExports().Count().Should().Be(Fixture.Module.Exports.Count(e => e is TableExport));
        }

        [Fact]
        public void ItCreatesExternsForTheTables()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var table1 = instance.GetTable("table1");
            table1.Should().NotBeNull();
            table1.Kind.Should().Be(ValueKind.FuncRef);
            table1.Minimum.Should().Be(1);
            table1.Maximum.Should().Be(10);

            var table2 = instance.GetTable("table2");
            table2.Should().NotBeNull();
            table2.Kind.Should().Be(ValueKind.FuncRef);
            table2.Minimum.Should().Be(10);
            table2.Maximum.Should().Be(uint.MaxValue);

            var table3 = instance.GetTable("table3");
            table3.Should().NotBeNull();
            table3.Kind.Should().Be(ValueKind.FuncRef);
            table3.Minimum.Should().Be(100);
            table3.Maximum.Should().Be(1000);
        }

        public static IEnumerable<object[]> GetTableExports()
        {
            yield return new object[] {
                "table1",
                ValueKind.FuncRef,
                1,
                10
            };

            yield return new object[] {
                "table2",
                ValueKind.FuncRef,
                10,
                uint.MaxValue
            };

            yield return new object[] {
                "table3",
                ValueKind.FuncRef,
                100,
                1000
            };
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
