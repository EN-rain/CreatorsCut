namespace Rushframe.Domain;

public sealed class SpeedCurve
{
    public double ConstantSpeed { get; set; } = 1.0;
    public bool PreservePitch { get; set; } = true;
    public List<SpeedSegment> Segments { get; init; } = [];

    public double MapSourceToTimeline(double sourceSeconds)
    {
        if (Segments.Count == 0) return sourceSeconds / ConstantSpeed;

        var timelineSeconds = 0.0;
        foreach (var segment in Segments.OrderBy(s => s.SourceStart.Seconds))
        {
            var start = segment.SourceStart.Seconds;
            var end = segment.SourceEnd.Seconds;
            if (sourceSeconds <= start) break;

            var coveredEnd = Math.Min(sourceSeconds, end);
            if (coveredEnd > start)
                timelineSeconds += (coveredEnd - start) / Math.Clamp(segment.Speed, 0.1, 100);

            if (sourceSeconds <= end)
                return timelineSeconds;
        }

        var lastEnd = Segments.Max(s => s.SourceEnd.Seconds);
        if (sourceSeconds > lastEnd)
            timelineSeconds += (sourceSeconds - lastEnd) / Math.Clamp(ConstantSpeed, 0.1, 100);

        return timelineSeconds;
    }
}

public sealed class SpeedSegment
{
    public MediaTime SourceStart { get; init; }
    public MediaTime SourceEnd { get; init; }
    public double Speed { get; set; } = 1.0;
}
