namespace CreatorCut.Domain;

public sealed class RippleState
{
    public bool Enabled { get; set; }

    public void ApplyDelete(Sequence sequence, Track track, TimelineItem deleted, MediaTime deleteStart, MediaTime deleteDuration)
    {
        if (!Enabled) return;

        var gapEnd = deleteStart.Add(deleteDuration);
        foreach (var item in track.Items.Where(i => i.TimelineStart >= gapEnd).OrderBy(i => i.TimelineStart.Seconds))
            item.TimelineStart = item.TimelineStart.Subtract(deleteDuration);
    }

    public void ApplyInsert(Sequence sequence, Track track, MediaTime insertAt, MediaTime insertDuration)
    {
        if (!Enabled) return;

        foreach (var item in track.Items.Where(i => i.TimelineStart >= insertAt).OrderByDescending(i => i.TimelineStart.Seconds))
            item.TimelineStart = item.TimelineStart.Add(insertDuration);
    }
}
