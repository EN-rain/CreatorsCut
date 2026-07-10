namespace CreatorCut.Domain;

public sealed class Marker
{
    public MarkerId Id { get; init; } = MarkerId.New();
    public required string Label { get; set; }
    public required MediaTime Time { get; set; }
    public string? Color { get; set; }
    public int DurationInFrames { get; set; }
}
