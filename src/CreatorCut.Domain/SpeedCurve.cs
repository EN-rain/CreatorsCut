namespace CreatorCut.Domain;

public sealed class SpeedCurve
{
    public double ConstantSpeed { get; set; } = 1.0;
    public bool PreservePitch { get; set; } = true;
    public List<SpeedSegment> Segments { get; init; } = [];

    public double MapSourceToTimeline(double sourceSeconds)
    {
        if (Segments.Count == 0) return sourceSeconds / ConstantSpeed;
        return sourceSeconds / ConstantSpeed;
    }
}

public sealed class SpeedSegment
{
    public MediaTime SourceStart { get; init; }
    public MediaTime SourceEnd { get; init; }
    public double Speed { get; set; } = 1.0;
}
