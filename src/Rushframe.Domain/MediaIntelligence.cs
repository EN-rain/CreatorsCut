namespace Rushframe.Domain;

public sealed class MediaIntelligenceAnalysis
{
    public MediaAssetId MediaAssetId { get; init; }
    public string SourcePath { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = "1.0";
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<MediaIntelligenceScene> Scenes { get; init; } = [];
    public List<MediaIntelligenceTranscriptSegment> Transcript { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

public sealed class MediaIntelligenceScene
{
    public string SceneId { get; init; } = string.Empty;
    public MediaTime Start { get; init; }
    public MediaTime End { get; init; }
    public string? Description { get; init; }
    public List<string> Tags { get; init; } = [];
    public double? VisualEnergy { get; init; }
}

public sealed class MediaIntelligenceTranscriptSegment
{
    public MediaTime Start { get; init; }
    public MediaTime End { get; init; }
    public string Text { get; init; } = string.Empty;
}
