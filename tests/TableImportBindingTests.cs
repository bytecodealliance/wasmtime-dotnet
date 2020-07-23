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

            Host.DefineFunction("", "assert", (string s1, string s2) => { s1.Should().Be(s2); });
        }

        private TableImportBindingFixture Fixture { get; set; }

        [Fact]
        public void ItFailsToInstantiateWithMissingImport()
        {
            Action action = () => { using var instance = Host.Instantiate(Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("unknown import: `::funcs` has not been defined");
        }

        [Fact]
        public void ItFailsToInstantiateWithTableTypeMismatch()
        {
            Host.DefineTable<string>("", "funcs", null, 1);
            Host.DefineTable<string>("", "externs", null, 1);
            Action action = () => { using var instance = Host.Instantiate(Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("*exported table incompatible with table import*");
        }

        [Fact]
        public void ItBindsTheTableCorrectly()
        {
            using var funcs = Host.DefineTable<Function>("", "funcs", null, 10);
            using var externs = Host.DefineTable<string>("", "externs", null, 10);

            using dynamic instance = Host.Instantiate(Fixture.Module);

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null_func(i) as object).Should().BeTrue();
                Convert.ToBoolean(instance.is_null_extern(i) as object).Should().BeTrue();
            }

            var called = new bool[10];

            for (int i = 0; i < 10; ++i)
            {
                int index = i;
                funcs[(uint)i] = Function.FromCallback(Store, () => { called[index] = true; });
                externs[(uint)i] = string.Format("string{0}", i);
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null_func(i) as object).Should().BeFalse();
                Convert.ToBoolean(instance.is_null_extern(i) as object).Should().BeFalse();
                instance.call(i);
                instance.assert_extern(i, string.Format("string{0}", i));
                funcs[(uint)i] = Function.Null;
                externs[(uint)i] = null;
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(instance.is_null_func(i) as object).Should().BeTrue();
                Convert.ToBoolean(instance.is_null_extern(i) as object).Should().BeTrue();
                called[i].Should().BeTrue();
            }
        }

        [Fact]
        public void ItGrowsATable()
        {
            using var funcs = Host.DefineTable<Function>("", "funcs", null, 10, 20);
            using var externs = Host.DefineTable<string>("", "externs", null, 10, 20);

            using dynamic instance = Host.Instantiate(Fixture.Module);

            funcs.Size.Should().Be(10);
            externs.Size.Should().Be(10);

            instance.grow_funcs(5);
            instance.grow_externs(5);

            funcs.Size.Should().Be(15);
            externs.Size.Should().Be(15);

            funcs.Grow(5, null).Should().Be(true);
            externs.Grow(5, null).Should().Be(true);

            funcs.Size.Should().Be(20);
            externs.Size.Should().Be(20);

            funcs.Grow(5, null).Should().Be(false);
            externs.Grow(5, null).Should().Be(false);
        }

        public void Dispose()
        {
            Store.Dispose();
            Host.Dispose();
        }
    }
}
