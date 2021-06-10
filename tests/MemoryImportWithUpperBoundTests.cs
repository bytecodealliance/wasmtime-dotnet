using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryImportWithUpperBoundFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryImportWithUpperBound.wat";
    }

    public class MemoryImportWithUpperBoundTests : IClassFixture<MemoryImportWithUpperBoundFixture>
    {
        public MemoryImportWithUpperBoundTests(MemoryImportWithUpperBoundFixture fixture)
        {
            Fixture = fixture;
        }

        private MemoryImportWithUpperBoundFixture Fixture { get; set; }

        [Fact]
        public void ItHasTheExpectedImport()
        {
            Fixture.Module.Imports.Count(i => i is MemoryImport).Should().Be(1);

            var memory = Fixture.Module.Imports[0] as MemoryImport;

            memory.Should().NotBeNull();
            memory.ModuleName.Should().Be("");
            memory.Name.Should().Be("mem");
            memory.Minimum.Should().Be(10);
            memory.Maximum.Should().Be(100);
        }
    }
}
