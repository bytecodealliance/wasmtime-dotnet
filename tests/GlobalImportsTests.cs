using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class GlobalImportsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "GlobalImports.wat";
    }

    public class GlobalImportsTests : IClassFixture<GlobalImportsFixture>
    {
        public GlobalImportsTests(GlobalImportsFixture fixture)
        {
            Fixture = fixture;
        }

        private GlobalImportsFixture Fixture { get; set; }

        [Theory]
        [MemberData(nameof(GetGlobalImports))]
        public void ItHasTheExpectedGlobalImports(string importModule, string importName, ValueKind expectedKind, Mutability expectedMutability)
        {
            var import = Fixture.Module.Imports.Where(f => f.ModuleName == importModule && f.Name == importName).FirstOrDefault() as GlobalImport;
            import.Should().NotBeNull();
            import.Kind.Should().Be(expectedKind);
            import.Mutability.Should().Be(expectedMutability);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedGlobals()
        {
            GetGlobalImports().Count().Should().Be(Fixture.Module.Imports.Count(i => i is GlobalImport));
        }

        public static IEnumerable<object[]> GetGlobalImports()
        {
            yield return new object[] {
                "",
                "global_i32",
                ValueKind.Int32,
                Mutability.Immutable
            };

            yield return new object[] {
                "",
                "global_i32_mut",
                ValueKind.Int32,
                Mutability.Mutable
            };

            yield return new object[] {
                "",
                "global_i64",
                ValueKind.Int64,
                Mutability.Immutable
            };

            yield return new object[] {
                "",
                "global_i64_mut",
                ValueKind.Int64,
                Mutability.Mutable
            };

            yield return new object[] {
                "",
                "global_f32",
                ValueKind.Float32,
                Mutability.Immutable
            };

            yield return new object[] {
                "",
                "global_f32_mut",
                ValueKind.Float32,
                Mutability.Mutable
            };

            yield return new object[] {
                "",
                "global_f64",
                ValueKind.Float64,
                Mutability.Immutable
            };

            yield return new object[] {
                "",
                "global_f64_mut",
                ValueKind.Float64,
                Mutability.Mutable
            };

            yield return new object[] {
                "other",
                "global_from_module",
                ValueKind.Int32,
                Mutability.Immutable
            };
        }
    }
}
