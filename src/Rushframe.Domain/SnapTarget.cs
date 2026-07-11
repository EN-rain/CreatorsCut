namespace Rushframe.Domain;

public readonly record struct SnapTarget(MediaTime Time, string Label)
{
    public static readonly double SnapThresholdSeconds = 0.5;

    public static List<SnapTarget> FromSequence(Sequence sequence)
    {
        var targets = new List<SnapTarget>();

        foreach (var track in sequence.Tracks)
        {
            foreach (var item in track.Items)
            {
                targets.Add(new SnapTarget(item.TimelineStart, $"Item {item.Id} start"));
                targets.Add(new SnapTarget(item.TimelineEnd, $"Item {item.Id} end"));
            }
        }

        foreach (var marker in sequence.Markers)
            targets.Add(new SnapTarget(marker.Time, $"Marker: {marker.Label}"));

        return targets;
    }

    public static MediaTime? FindSnap(MediaTime cursor, List<SnapTarget> targets)
    {
        MediaTime? best = null;
        var bestDist = double.MaxValue;

        foreach (var t in targets)
        {
            var dist = Math.Abs((cursor - t.Time).Seconds);
            if (dist < SnapThresholdSeconds && dist < bestDist)
            {
                best = t.Time;
                bestDist = dist;
            }
        }

        return best;
    }
}
