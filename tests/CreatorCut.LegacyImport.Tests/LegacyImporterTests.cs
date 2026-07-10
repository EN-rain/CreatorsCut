namespace CreatorCut.LegacyImport.Tests;

public sealed class LegacyImporterTests
{
    private static readonly string FixtureRoot = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "Fixtures");

    [Fact]
    public void import_golden_fixture()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);

        Assert.False(result.HasErrors);
        Assert.Equal("Golden Test Project", result.ProjectName);
        Assert.NotNull(result.Project);
    }

    [Fact]
    public void import_maps_assets()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);

        Assert.Equal(1, result.MappedAssets);
    }

    [Fact]
    public void import_maps_clips()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);

        Assert.Equal(1, result.MappedClips);
    }

    [Fact]
    public void import_maps_markers()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);

        Assert.Equal(1, result.MappedMarkers);
    }

    [Fact]
    public void import_generates_report()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);

        var report = result.GenerateReport();
        Assert.Contains("Golden Test Project", report);
        Assert.Contains("Assets mapped: 1", report);
        Assert.Contains("Clips mapped: 1", report);
    }

    [Fact]
    public void import_missing_project_returns_error()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.True(result.HasErrors);
    }

    [Fact]
    public void import_round_trip_serialization()
    {
        var importer = new LegacyImporter();
        var result = importer.ImportProject(FixtureRoot);
        Assert.NotNull(result.Project);

        var json = Domain.Serialization.ProjectSerializer.Serialize(result.Project);
        var restored = Domain.Serialization.ProjectSerializer.Deserialize(json);

        Assert.Equal(result.ProjectName, restored.Name);
        Assert.Single(restored.MainSequence!.Tracks);
    }
}
