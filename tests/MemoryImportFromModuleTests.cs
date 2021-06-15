using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryImportFromModuleFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryImportFromModule.wat";
    }

    public class MemoryImportFromModuleTests : IClassFixture<MemoryImportFromModuleFixture>
    {
        public MemoryImportFromModuleTests(MemoryImportFromModuleFixture fixture)
        {
            Fixture = fixture;
        }

        private MemoryImportFromModuleFixture Fixture { get; set; }

        [Fact]
        public void ItHasTheExpectedImport()
        {
            Fixture.Module.Imports.Count(i => i is MemoryImport).Should().Be(1);

            var memory = Fixture.Module.Imports[0] as MemoryImport;

            memory.Should().NotBeNull();
            memory.ModuleName.Should().Be("js");
            memory.Name.Should().Be("mem");
            memory.Minimum.Should().Be(1);
            memory.Maximum.Should().Be(2);
        }
    }
}
