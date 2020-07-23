using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TableImportBindingFixture : ModuleFixture
    {
        protected override string ModuleFileName => "TableImportBinding.wat";
    }

    public class TableImportBindingTests : IClassFixture<TableImportBindingFixture>, IDisposable
    {
        private Store Store { get; set; }
        private Host Host { get; set; }

        public TableImportBindingTests(TableImportBindingFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Host = new Host(Store);
        }

        private TableImportBindingFixture Fixture { get; set; }

        [Fact]
        public void ItFailsToInstantiateWithMissingImport()
        {
            Action action = () => { using var instance = Host.Instantiate(Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("unknown import: `::table` has not been defined");
        }

        [Fact]
        public void ItFailsToInstantiateWithTableTypeMismatch()
        {
            Host.DefineTable<string>("", "table", null, 1);
            Action action = () => { using var instance = Host.Instantiate(Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("*exported table incompatible with table import*");
        }

        [Fact]
        public void ItBindsTheTableCorrectly()
        {
            using var table = Host.DefineTable<Function>("", "table", null, 10);

            using dynamic instance = Host.Instantiate(Fixture.Module);

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null(i) as object).Should().BeTrue();
            }

            var called = new bool[10];

            for (int i = 0; i < 10; ++i)
            {
                int index = i;
                table[(uint)i] = Function.FromCallback(Store, () => { called[index] = true; });
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null(i) as object).Should().BeFalse();
                instance.call(i);
                table[(uint)i] = Function.Null;
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null(i) as object).Should().BeTrue();
                called[i].Should().BeTrue();
            }
        }

        [Fact]
        public void ItGrowsATable()
        {
            using var table = Host.DefineTable<Function>("", "table", null, 10, 20);

            using dynamic instance = Host.Instantiate(Fixture.Module);

            table.Size.Should().Be(10);

            instance.grow(5);

            table.Size.Should().Be(15);

            table.Grow(5, null).Should().Be(true);

            table.Size.Should().Be(20);

            table.Grow(5, null).Should().Be(false);
        }

        public void Dispose()
        {
            Store.Dispose();
            Host.Dispose();
        }
    }
}
