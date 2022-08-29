using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class GlobalExportsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "GlobalExports.wat";
    }

    public class GlobalExportsTests : IClassFixture<GlobalExportsFixture>, IDisposable
    {
        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public GlobalExportsTests(GlobalExportsFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        private GlobalExportsFixture Fixture { get; set; }

        [Theory]
        [MemberData(nameof(GetGlobalExports))]
        public void ItHasTheExpectedGlobalExports(string exportName, ValueKind expectedKind, Mutability expectedMutability)
        {
            var export = Fixture.Module.Exports.Where(f => f.Name == exportName).FirstOrDefault() as GlobalExport;
            export.Should().NotBeNull();
            export.Kind.Should().Be(expectedKind);
            export.Mutability.Should().Be(expectedMutability);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedGlobals()
        {
            GetGlobalExports().Count().Should().Be(Fixture.Module.Exports.Count(e => e is GlobalExport));
        }

        [Fact]
        public void ItCreatesExternsForTheGlobals()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var i32 = instance.GetGlobal("global_i32");
            i32.Kind.Should().Be(ValueKind.Int32);
            i32.Mutability.Should().Be(Mutability.Immutable);
            i32.GetValue().Should().Be(0);

            var i32Mut = instance.GetGlobal("global_i32_mut");
            i32Mut.Kind.Should().Be(ValueKind.Int32);
            i32Mut.Mutability.Should().Be(Mutability.Mutable);
            i32Mut.GetValue().Should().Be(1);
            i32Mut.SetValue(11);
            i32Mut.GetValue().Should().Be(11);

            var i64 = instance.GetGlobal("global_i64");
            i64.Kind.Should().Be(ValueKind.Int64);
            i64.Mutability.Should().Be(Mutability.Immutable);
            i64.GetValue().Should().Be(2);

            var i64Mut = instance.GetGlobal("global_i64_mut");
            i64Mut.Kind.Should().Be(ValueKind.Int64);
            i64Mut.Mutability.Should().Be(Mutability.Mutable);
            i64Mut.GetValue().Should().Be(3);
            i64Mut.SetValue(13);
            i64Mut.GetValue().Should().Be(13);

            var f32 = instance.GetGlobal("global_f32");
            f32.Kind.Should().Be(ValueKind.Float32);
            f32.Mutability.Should().Be(Mutability.Immutable);
            f32.GetValue().Should().Be(4);

            var f32Mut = instance.GetGlobal("global_f32_mut");
            f32Mut.Kind.Should().Be(ValueKind.Float32);
            f32Mut.Mutability.Should().Be(Mutability.Mutable);
            f32Mut.GetValue().Should().Be(5);
            f32Mut.SetValue(15);
            f32Mut.GetValue().Should().Be(15);

            var f64 = instance.GetGlobal("global_f64");
            f64.Kind.Should().Be(ValueKind.Float64);
            f64.Mutability.Should().Be(Mutability.Immutable);
            f64.GetValue().Should().Be(6);

            var f64Mut = instance.GetGlobal("global_f64_mut");
            f64Mut.Kind.Should().Be(ValueKind.Float64);
            f64Mut.Mutability.Should().Be(Mutability.Mutable);
            f64Mut.GetValue().Should().Be(7);
            f64Mut.SetValue(17);
            f64Mut.GetValue().Should().Be(17);

            Action action = () => i32.SetValue(0);
            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The global is immutable and cannot be changed.");

            action = () => i64.SetValue(0);
            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The global is immutable and cannot be changed.");

            action = () => f32.SetValue(0);
            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The global is immutable and cannot be changed.");

            action = () => f64.SetValue(0);
            action
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The global is immutable and cannot be changed.");
        }

        public static IEnumerable<object[]> GetGlobalExports()
        {
            yield return new object[] {
                "global_i32",
                ValueKind.Int32,
                Mutability.Immutable
            };

            yield return new object[] {
                "global_i32_mut",
                ValueKind.Int32,
                Mutability.Mutable
            };

            yield return new object[] {
                "global_i64",
                ValueKind.Int64,
                Mutability.Immutable
            };

            yield return new object[] {
                "global_i64_mut",
                ValueKind.Int64,
                Mutability.Mutable
            };

            yield return new object[] {
                "global_f32",
                ValueKind.Float32,
                Mutability.Immutable
            };

            yield return new object[] {
                "global_f32_mut",
                ValueKind.Float32,
                Mutability.Mutable
            };

            yield return new object[] {
                "global_f64",
                ValueKind.Float64,
                Mutability.Immutable
            };

            yield return new object[] {
                "global_f64_mut",
                ValueKind.Float64,
                Mutability.Mutable
            };
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
