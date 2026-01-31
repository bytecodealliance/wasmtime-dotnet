using System;
using System.Runtime.InteropServices;
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
        private TableImportBindingFixture Fixture { get; set; }
        private Store Store { get; set; }
        private Linker Linker { get; set; }
        private static bool IsMacArm64 =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

        public TableImportBindingTests(TableImportBindingFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);

            Linker.Define("", "assert", Function.FromCallback(Store, (string s1, string s2) => { s1.Should().Be(s2); }));
        }

        [Fact]
        public void ItFailsToInstantiateWithMissingImport()
        {
            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("unknown import: `::funcs` has not been defined");
        }

        [Fact]
        public void ItFailsToInstantiateWithTableTypeMismatch()
        {
            var funcs = new Table(Store, TableKind.ExternRef, null, 10);
            var externs = new Table(Store, TableKind.ExternRef, null, 10);

            Linker.Define("", "funcs", funcs);
            Linker.Define("", "externs", externs);

            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("incompatible import type for `::funcs`*");
        }

        [Fact]
        public void ItFailsToInstantiateWithTableLimitsMismatch()
        {
            var funcs = new Table(Store, TableKind.FuncRef, null, 10);
            var externs = new Table(Store, TableKind.ExternRef, null, 1);

            Linker.Define("", "funcs", funcs);
            Linker.Define("", "externs", externs);

            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("incompatible import type for `::externs`*");
        }

        [Fact]
        public void ItBindsTheTableCorrectly()
        {
            if (IsMacArm64)
            {
                return;
            }

            var funcs = new Table(Store, TableKind.FuncRef, null, 10);
            var externs = new Table(Store, TableKind.ExternRef, null, 10);

            Linker.Define("", "funcs", funcs);
            Linker.Define("", "externs", externs);

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var is_null_func = instance.GetFunction<int, int>("is_null_func");
            var is_null_extern = instance.GetFunction<int, int>("is_null_extern");
            var call = instance.GetAction<int>("call");
            var assert_extern = instance.GetAction<int, string>("assert_extern");

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(is_null_func(i)).Should().BeTrue();
                Convert.ToBoolean(is_null_extern(i)).Should().BeTrue();
            }

            var called = new bool[10];

            for (int i = 0; i < 10; ++i)
            {
                int index = i;
                funcs.SetElement((uint)i, Function.FromCallback(Store, () => { called[index] = true; }));
                externs.SetElement((uint)i, string.Format("string{0}", i));
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(is_null_func(i)).Should().BeFalse();
                Convert.ToBoolean(is_null_extern(i)).Should().BeFalse();
                call!(i);
                assert_extern!(i, string.Format("string{0}", i));
                funcs.SetElement((uint)i, Function.Null);
                externs.SetElement((uint)i, null);
            }

            for (int i = 0; i < 10; ++i)
            {
                Convert.ToBoolean(is_null_func(i)).Should().BeTrue();
                Convert.ToBoolean(is_null_extern(i)).Should().BeTrue();
                called[i].Should().BeTrue();
            }
        }

        [Fact]
        public void ItGrowsATable()
        {
            if (IsMacArm64)
            {
                return;
            }

            var funcs = new Table(Store, TableKind.FuncRef, null, 10, 20);
            var externs = new Table(Store, TableKind.ExternRef, null, 10, 20);

            Linker.Define("", "funcs", funcs);
            Linker.Define("", "externs", externs);

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var grow_funcs = instance.GetAction<int>("grow_funcs");
            var grow_externs = instance.GetAction<int>("grow_externs");

            funcs.GetSize().Should().Be(10);
            externs.GetSize().Should().Be(10);

            grow_funcs!(5);
            grow_externs!(3);

            funcs.GetSize().Should().Be(15);
            externs.GetSize().Should().Be(13);

            funcs.Grow(5, null).Should().Be(15);
            externs.Grow(5, null).Should().Be(13);

            funcs.GetSize().Should().Be(20);
            externs.GetSize().Should().Be(18);

            Action action = () => { funcs.Grow(5, null); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("failed to grow table by `5`");

            action = () => { externs.Grow(3, null); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("failed to grow table by `3`");
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
