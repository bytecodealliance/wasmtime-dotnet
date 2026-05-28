using System;
using System.Linq;
using Xunit;

namespace Wasmtime.Tests;

public class ExportTagsFixture
    : ModuleFixture
{
    protected override string ModuleFileName => "ExportTags.wat";

    public override Config GetEngineConfig()
    {
        return base.GetEngineConfig()
                   .WithExceptions(true);
    }
}

public sealed class ExportTagsTests
    : IClassFixture<ExportTagsFixture>, IDisposable
{
    private ExportTagsFixture Fixture { get; set; }

    private Store Store { get; set; }

    private Linker Linker { get; set; }

    public ExportTagsTests(ExportTagsFixture fixture)
    {
        Fixture = fixture;
        Store = new Store(Fixture.Engine);
        Linker = new Linker(Fixture.Engine);
    }

    public void Dispose()
    {
        Store.Dispose();
        Linker.Dispose();
    }

    [Fact]
    public void ItExportsTags()
    {
        Assert.Single(Fixture.Module.Exports);

        var export = (TagExport)Fixture.Module.Exports.Single();

        Assert.Equal("$export_tag", export.Name);

        Assert.Single(export.Parameters);
        Assert.Equal(ValueKind.Int32, export.Parameters.Single());
    }

    [Fact]
    public void ItImportsTags()
    {
        Assert.Single(Fixture.Module.Imports);

        var export = (TagImport)Fixture.Module.Imports.Single();

        Assert.Equal("$import_tag", export.Name);

        Assert.Single(export.Parameters);
        Assert.Equal(ValueKind.Int32, export.Parameters.Single());
    }
}