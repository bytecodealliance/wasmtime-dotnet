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

    public class TableExportsTests : IClassFixture<TableExportsFixture>
    {
        public TableExportsTests(TableExportsFixture fixture)
        {
            Fixture = fixture;
        }

        private TableExportsFixture Fixture { get; set; }

        [Theory]
        [MemberData(nameof(GetTableExports))]
        public void ItHasTheExpectedTableExports(string exportName, ValueKind expectedKind, uint expectedMinimum, uint expectedMaximum)
        {
            var export = Fixture.Module.Exports.Tables.Where(f => f.Name == exportName).FirstOrDefault();
            export.Should().NotBeNull();
            export.Kind.Should().Be(expectedKind);
            export.Minimum.Should().Be(expectedMinimum);
            export.Maximum.Should().Be(expectedMaximum);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedTables()
        {
            GetTableExports().Count().Should().Be(Fixture.Module.Exports.Tables.Count);
        }

        [Fact]
        public void ItCreatesExternsForTheGlobals()
        {
            using var instance = Fixture.Host.Instantiate(Fixture.Module);

            var tables = instance.Externs.Tables;
            tables.Count.Should().Be(3);

            var table1 = tables[0];
            table1.Name.Should().Be("table1");
            table1.Kind.Should().Be(ValueKind.FuncRef);
            table1.Minimum.Should().Be(1);
            table1.Maximum.Should().Be(10);

            var table2 = tables[1];
            table2.Name.Should().Be("table2");
            table2.Kind.Should().Be(ValueKind.FuncRef);
            table2.Minimum.Should().Be(10);
            table2.Maximum.Should().Be(uint.MaxValue);

            var table3 = tables[2];
            table3.Name.Should().Be("table3");
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
    }
}
