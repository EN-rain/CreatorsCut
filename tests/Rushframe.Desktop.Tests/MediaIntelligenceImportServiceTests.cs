using Rushframe.Infrastructure;
using Rushframe.Domain;

namespace Rushframe.Desktop.Tests;

public sealed class MediaIntelligenceImportServiceTests
{
    [Fact]
    public async Task import_reads_pipeline_json_and_skips_invalid_segments()
    {
        var path = Path.Combine(Path.GetTempPath(), $"media-analysis-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, """
        {
          "source_path": "sample.mp4",
          "schema_version": "1.0",
          "scenes": [
            { "scene_id": "scene-1", "start": 1.0, "end": 3.5, "description": "Opening", "tags": ["wide", "wide"], "visual_energy": 0.8 },
            { "scene_id": "bad", "start": 4.0, "end": 2.0 }
          ],
          "transcript": [
            { "start": 1.2, "end": 2.4, "text": "  Hello there  " },
            { "start": 3.0, "end": 3.0, "text": "invalid" }
          ],
          "warnings": ["test warning"]
        }
        """);

        try
        {
            var asset = new MediaAsset { Kind = MediaKind.Video, OriginalPath = "sample.mp4" };
            var service = new MediaIntelligenceImportService();

            var analysis = await service.ImportAsync(path, asset);

            Assert.Equal(asset.Id, analysis.MediaAssetId);
            Assert.Single(analysis.Scenes);
            Assert.Equal("Opening", analysis.Scenes[0].Description);
            Assert.Single(analysis.Scenes[0].Tags);
            Assert.Single(analysis.Transcript);
            Assert.Equal("Hello there", analysis.Transcript[0].Text);
            Assert.Equal(3, analysis.Warnings.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void store_in_project_replaces_analysis_for_same_asset()
    {
        var project = new Project();
        var asset = new MediaAsset { Kind = MediaKind.Video, OriginalPath = "sample.mp4" };
        project.MediaLibrary.Add(asset);
        project.MediaIntelligence.Add(new MediaIntelligenceAnalysis { MediaAssetId = asset.Id, SourcePath = "old" });

        MediaIntelligenceImportService.StoreInProject(project, new MediaIntelligenceAnalysis
        {
            MediaAssetId = asset.Id,
            SourcePath = "new",
        });

        var stored = Assert.Single(project.MediaIntelligence);
        Assert.Equal("new", stored.SourcePath);
    }
}
