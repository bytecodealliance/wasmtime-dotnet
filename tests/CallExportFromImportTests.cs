using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class CallExportFromImportFixture : ModuleFixture
    {
        protected override string ModuleFileName => "CallExportFromImport.wat";
    }

    public class CallExportFromImportTests : IClassFixture<CallExportFromImportFixture>, IDisposable
    {
        private CallExportFromImportFixture Fixture { get; }
        private Store Store { get; }
        private Linker Linker { get; }

        public CallExportFromImportTests(CallExportFromImportFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItCallsExportedFunctionFromImportedFunction()
        {
            Linker.DefineFunction("env", "getInt", (Caller caller, int arg) =>
            {
                var shiftLeftFunc = caller.GetFunction("shiftLeft");

                return (int)shiftLeftFunc.Invoke(arg);
            });

            var instance = Linker.Instantiate(Store, Fixture.Module);
            var testFunction = instance.GetFunction("testFunction");

            var result = (int)testFunction.Invoke(2);
            result.Should().Be(2 << 1);
        }

        public void Dispose()
        {
            Store?.Dispose();
            Linker?.Dispose();
        }
    }
}