using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class GlobalImportBindingFixture : ModuleFixture
    {
        protected override string ModuleFileName => "GlobalImportBindings.wat";
    }

    public class GlobalImportBindingTests : IClassFixture<GlobalImportBindingFixture>, IDisposable
    {
        private GlobalImportBindingFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public GlobalImportBindingTests(GlobalImportBindingFixture fixture)
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
                .WithMessage("unknown import: `::global_i32_mut` has not been defined");
        }

        [Fact]
        public void ItFailsToDefineAGlobalWithInvalidValue()
        {
            Action action = () => { Linker.Define("", "global_i32_mut", new Global(Store, ValueKind.Int32, null, Mutability.Mutable)); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("The value `null` is not valid for WebAssembly type Int32.");
        }

        [Fact]
        public void ItFailsToInstantiateWithGlobalTypeMismatch()
        {
            Linker.Define("", "global_i32_mut", new Global(Store, ValueKind.Int64, 0, Mutability.Mutable));
            Linker.Define("", "global_i32", new Global(Store, ValueKind.Int32, 0, Mutability.Immutable));
            Linker.Define("", "global_i64_mut", new Global(Store, ValueKind.Int64, 0, Mutability.Mutable));
            Linker.Define("", "global_i64", new Global(Store, ValueKind.Int64, 0, Mutability.Immutable));
            Linker.Define("", "global_f32_mut", new Global(Store, ValueKind.Float32, 0, Mutability.Mutable));
            Linker.Define("", "global_f32", new Global(Store, ValueKind.Float32, 0, Mutability.Immutable));
            Linker.Define("", "global_f64_mut", new Global(Store, ValueKind.Float64, 0, Mutability.Mutable));
            Linker.Define("", "global_f64", new Global(Store, ValueKind.Float64, 0, Mutability.Immutable));

            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("incompatible import type for `::global_i32_mut`*");
        }

        [Fact]
        public void ItFailsToInstantiateWhenGlobalIsNotMut()
        {
            Linker.Define("", "global_i32_mut", new Global(Store, ValueKind.Int32, 0, Mutability.Immutable));
            Linker.Define("", "global_i32", new Global(Store, ValueKind.Int32, 0, Mutability.Immutable));
            Linker.Define("", "global_i64_mut", new Global(Store, ValueKind.Int64, 0, Mutability.Mutable));
            Linker.Define("", "global_i64", new Global(Store, ValueKind.Int64, 0, Mutability.Immutable));
            Linker.Define("", "global_f32_mut", new Global(Store, ValueKind.Float32, 0, Mutability.Mutable));
            Linker.Define("", "global_f32", new Global(Store, ValueKind.Float32, 0, Mutability.Immutable));
            Linker.Define("", "global_f64_mut", new Global(Store, ValueKind.Float64, 0, Mutability.Mutable));
            Linker.Define("", "global_f64", new Global(Store, ValueKind.Float64, 0, Mutability.Immutable));

            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("incompatible import type for `::global_i32_mut`*");
        }

        [Fact]
        public void ItFailsToInstantiateWhenGlobalIsMut()
        {
            Linker.Define("", "global_i32_mut", new Global(Store, ValueKind.Int32, 0, Mutability.Mutable));
            Linker.Define("", "global_i32", new Global(Store, ValueKind.Int32, 0, Mutability.Mutable));
            Linker.Define("", "global_i64_mut", new Global(Store, ValueKind.Int64, 0, Mutability.Mutable));
            Linker.Define("", "global_i64", new Global(Store, ValueKind.Int64, 0, Mutability.Immutable));
            Linker.Define("", "global_f32_mut", new Global(Store, ValueKind.Float32, 0, Mutability.Mutable));
            Linker.Define("", "global_f32", new Global(Store, ValueKind.Float32, 0, Mutability.Immutable));
            Linker.Define("", "global_f64_mut", new Global(Store, ValueKind.Float64, 0, Mutability.Mutable));
            Linker.Define("", "global_f64", new Global(Store, ValueKind.Float64, 0, Mutability.Immutable));

            Action action = () => { Linker.Instantiate(Store, Fixture.Module); };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("incompatible import type for `::global_i32`*");
        }

        [Fact]
        public void ItBindsTheGlobalsCorrectly()
        {
            var global_i32_mut = new Global(Store, ValueKind.Int32, 0, Mutability.Mutable).Wrap<int>();
            var global_i32 = new Global(Store, ValueKind.Int32, 1, Mutability.Immutable).Wrap<int>();
            var global_i64_mut = new Global(Store, ValueKind.Int64, 2, Mutability.Mutable).Wrap<long>();
            var global_i64 = new Global(Store, ValueKind.Int64, 3, Mutability.Immutable).Wrap<long>();
            var global_f32_mut = new Global(Store, ValueKind.Float32, 4, Mutability.Mutable).Wrap<float>();
            var global_f32 = new Global(Store, ValueKind.Float32, 5, Mutability.Immutable).Wrap<float>();
            var global_f64_mut = new Global(Store, ValueKind.Float64, 6, Mutability.Mutable).Wrap<double>();
            var global_f64 = new Global(Store, ValueKind.Float64, 7, Mutability.Immutable).Wrap<double>();

            Linker.Define("", "global_i32_mut", global_i32_mut);
            Linker.Define("", "global_i32", global_i32);
            Linker.Define("", "global_i64_mut", global_i64_mut);
            Linker.Define("", "global_i64", global_i64);
            Linker.Define("", "global_f32_mut", global_f32_mut);
            Linker.Define("", "global_f32", global_f32);
            Linker.Define("", "global_f64_mut", global_f64_mut);
            Linker.Define("", "global_f64", global_f64);

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var get_global_i32_mut = instance.GetFunction("get_global_i32_mut");
            var set_global_i32_mut = instance.GetFunction("set_global_i32_mut");
            var get_global_i32 = instance.GetFunction("get_global_i32");
            var get_global_i64_mut = instance.GetFunction("get_global_i64_mut");
            var set_global_i64_mut = instance.GetFunction("set_global_i64_mut");
            var get_global_i64 = instance.GetFunction("get_global_i64");
            var get_global_f32_mut = instance.GetFunction("get_global_f32_mut");
            var set_global_f32_mut = instance.GetFunction("set_global_f32_mut");
            var get_global_f32 = instance.GetFunction("get_global_f32");
            var get_global_f64_mut = instance.GetFunction("get_global_f64_mut");
            var set_global_f64_mut = instance.GetFunction("set_global_f64_mut");
            var get_global_f64 = instance.GetFunction("get_global_f64");

            global_i32_mut.GetValue().Should().Be(0);
            get_global_i32_mut.Invoke().Should().Be(0);
            global_i32.GetValue().Should().Be(1);
            get_global_i32.Invoke().Should().Be(1);
            global_i64_mut.GetValue().Should().Be(2);
            get_global_i64_mut.Invoke().Should().Be(2);
            global_i64.GetValue().Should().Be(3);
            get_global_i64.Invoke().Should().Be(3);
            global_f32_mut.GetValue().Should().Be(4);
            get_global_f32_mut.Invoke().Should().Be(4);
            global_f32.GetValue().Should().Be(5);
            get_global_f32.Invoke().Should().Be(5);
            global_f64_mut.GetValue().Should().Be(6);
            get_global_f64_mut.Invoke().Should().Be(6);
            global_f64.GetValue().Should().Be(7);
            get_global_f64.Invoke().Should().Be(7);

            global_i32_mut.SetValue(10);
            global_i32_mut.GetValue().Should().Be(10);
            set_global_i32_mut.Invoke(11);
            get_global_i32_mut.Invoke().Should().Be(11);

            global_i64_mut.SetValue(12);
            global_i64_mut.GetValue().Should().Be(12);
            set_global_i64_mut.Invoke(13);
            get_global_i64_mut.Invoke().Should().Be(13);

            global_f32_mut.SetValue(14);
            global_f32_mut.GetValue().Should().Be(14);
            set_global_f32_mut.Invoke(15);
            get_global_f32_mut.Invoke().Should().Be(15);

            global_f64_mut.SetValue(16);
            global_f64_mut.GetValue().Should().Be(16);
            set_global_f64_mut.Invoke(17);
            get_global_f64_mut.Invoke().Should().Be(17);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
