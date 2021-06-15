using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MemoryImportNoUpperBoundFixture : ModuleFixture
    {
        protected override string ModuleFileName => "MemoryImportNoUpperBound.wat";
    }

    public class MemoryImportNoUpperBoundTests : IClassFixture<MemoryImportNoUpperBoundFixture>
    {
        public MemoryImportNoUpperBoundTests(MemoryImportNoUpperBoundFixture fixture)
        {
            Fixture = fixture;
        }

        private MemoryImportNoUpperBoundFixture Fixture { get; set; }

        [Fact]
        public void ItHasTheExpectedImport()
        {
            Fixture.Module.Imports.Count(i => i is MemoryImport).Should().Be(1);

            var memory = Fixture.Module.Imports[0] as MemoryImport;

            memory.Should().NotBeNull();
            memory.ModuleName.Should().Be("");
            memory.Name.Should().Be("mem");
            memory.Minimum.Should().Be(1);
            memory.Maximum.Should().Be(uint.MaxValue);
        }
    }
}
