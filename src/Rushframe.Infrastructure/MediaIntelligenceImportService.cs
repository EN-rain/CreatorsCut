using System.Text.Json;
using Rushframe.Domain;

namespace Rushframe.Infrastructure;

public sealed class MediaIntelligenceImportService
{
    public async Task<MediaIntelligenceAnalysis> ImportAsync(
        string analysisPath,
        MediaAsset asset,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(analysisPath))
            throw new ArgumentException("Analysis path is required", nameof(analysisPath));
        if (!File.Exists(analysisPath))
            throw new FileNotFoundException("Media analysis file was not found", analysisPath);

        await using var stream = File.OpenRead(analysisPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Media analysis JSON root must be an object");

        var analysis = new MediaIntelligenceAnalysis
        {
            MediaAssetId = asset.Id,
            SourcePath = ReadString(root, "source_path") ?? asset.OriginalPath,
            SchemaVersion = ReadString(root, "schema_version") ?? "1.0",
            ImportedAt = DateTimeOffset.UtcNow,
        };

        if (root.TryGetProperty("scenes", out var scenes) && scenes.ValueKind == JsonValueKind.Array)
        {
            foreach (var source in scenes.EnumerateArray())
            {
                var start = ReadDouble(source, "start");
                var end = ReadDouble(source, "end");
                var sceneId = ReadString(source, "scene_id") ?? string.Empty;
                if (!IsValidRange(start, end))
                {
                    analysis.Warnings.Add($"Skipped scene '{sceneId}' because its time range is invalid.");
                    continue;
                }

                analysis.Scenes.Add(new MediaIntelligenceScene
                {
                    SceneId = sceneId,
                    Start = MediaTime.FromSeconds(start),
                    End = MediaTime.FromSeconds(end),
                    Description = ReadString(source, "description"),
                    Tags = ReadStrings(source, "tags"),
                    VisualEnergy = ReadNullableDouble(source, "visual_energy"),
                });
            }
        }

        if (root.TryGetProperty("transcript", out var transcript) && transcript.ValueKind == JsonValueKind.Array)
        {
            foreach (var source in transcript.EnumerateArray())
            {
                var start = ReadDouble(source, "start");
                var end = ReadDouble(source, "end");
                var text = ReadString(source, "text");
                if (!IsValidRange(start, end) || string.IsNullOrWhiteSpace(text))
                {
                    analysis.Warnings.Add("Skipped a transcript segment because its text or time range is invalid.");
                    continue;
                }

                analysis.Transcript.Add(new MediaIntelligenceTranscriptSegment
                {
                    Start = MediaTime.FromSeconds(start),
                    End = MediaTime.FromSeconds(end),
                    Text = text.Trim(),
                });
            }
        }

        if (root.TryGetProperty("warnings", out var warnings) && warnings.ValueKind == JsonValueKind.Array)
        {
            analysis.Warnings.AddRange(warnings.EnumerateArray()
                .Where(element => element.ValueKind == JsonValueKind.String)
                .Select(element => element.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!));
        }

        return analysis;
    }

    public static void StoreInProject(Project project, MediaIntelligenceAnalysis analysis)
    {
        project.MediaIntelligence.RemoveAll(existing => existing.MediaAssetId == analysis.MediaAssetId);
        project.MediaIntelligence.Add(analysis);
    }

    private static bool IsValidRange(double start, double end) =>
        double.IsFinite(start) && double.IsFinite(end) && start >= 0 && end > start;

    private static string? ReadString(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double ReadDouble(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.TryGetDouble(out var result)
            ? result
            : double.NaN;

    private static double? ReadNullableDouble(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.TryGetDouble(out var result)
            ? result
            : null;

    private static List<string> ReadStrings(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];

        return value.EnumerateArray()
            .Where(element => element.ValueKind == JsonValueKind.String)
            .Select(element => element.GetString())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
