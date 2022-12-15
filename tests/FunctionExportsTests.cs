using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class FunctionExportsFixture : ModuleFixture
    {
        protected override string ModuleFileName => "FunctionExports.wat";
    }

    public class FunctionExportsTests : IClassFixture<FunctionExportsFixture>
    {
        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public FunctionExportsTests(FunctionExportsFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        private FunctionExportsFixture Fixture { get; set; }

        [Theory]
        [MemberData(nameof(GetFunctionExports))]
        public void ItHasTheExpectedFunctionExports(string exportName, ValueKind[] expectedParameters, ValueKind[] expectedResults)
        {
            var export = Fixture.Module.Exports.Where(f => f.Name == exportName).FirstOrDefault() as FunctionExport;
            export.Should().NotBeNull();
            export.Parameters.Should().Equal(expectedParameters);
            export.Results.Should().Equal(expectedResults);
        }

        [Fact]
        public void ItHasTheExpectedNumberOfExportedFunctions()
        {
            GetFunctionExports().Count().Should().Be(Fixture.Module.Exports.Count(e => e is FunctionExport));
        }

        [Fact]
        public void ItReturnsNullForNonExistantFunction()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var i32 = instance.GetFunction("no_such_func");
            i32.Should().BeNull();
        }

        [Fact]
        public void ItReturnsNullForWrongTypeSignature()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);

            var i32 = instance.GetFunction("one_i32_param_no_results", typeof(float));
            i32.Should().BeNull();
        }

        public static IEnumerable<object[]> GetFunctionExports()
        {
            yield return new object[] {
                "no_params_no_results",
                Array.Empty<ValueKind>(),
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "one_i32_param_no_results",
                new ValueKind[] {
                    ValueKind.Int32
                },
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "one_i64_param_no_results",
                new ValueKind[] {
                    ValueKind.Int64
                },
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "one_f32_param_no_results",
                new ValueKind[] {
                    ValueKind.Float32
                },
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "one_f64_param_no_results",
                new ValueKind[] {
                    ValueKind.Float64
                },
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "one_param_of_each_type",
                new ValueKind[] {
                    ValueKind.Int32,
                    ValueKind.Int64,
                    ValueKind.Float32,
                    ValueKind.Float64
                },
                Array.Empty<ValueKind>()
            };

            yield return new object[] {
                "no_params_one_i32_result",
                Array.Empty<ValueKind>(),
                new ValueKind[] {
                    ValueKind.Int32,
                }
            };

            yield return new object[] {
                "no_params_one_i64_result",
                Array.Empty<ValueKind>(),
                new ValueKind[] {
                    ValueKind.Int64,
                }
            };

            yield return new object[] {
                "no_params_one_f32_result",
                Array.Empty<ValueKind>(),
                new ValueKind[] {
                    ValueKind.Float32,
                }
            };

            yield return new object[] {
                "no_params_one_f64_result",
                Array.Empty<ValueKind>(),
                new ValueKind[] {
                    ValueKind.Float64,
                }
            };

            yield return new object[] {
                "one_result_of_each_type",
                Array.Empty<ValueKind>(),
                new ValueKind[] {
                    ValueKind.Int32,
                    ValueKind.Int64,
                    ValueKind.Float32,
                    ValueKind.Float64,
                }
            };

            yield return new object[] {
                "one_param_and_result_of_each_type",
                new ValueKind[] {
                    ValueKind.Int32,
                    ValueKind.Int64,
                    ValueKind.Float32,
                    ValueKind.Float64,
                },
                new ValueKind[] {
                    ValueKind.Int32,
                    ValueKind.Int64,
                    ValueKind.Float32,
                    ValueKind.Float64,
                }
            };
        }
    }
}
