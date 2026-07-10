namespace CreatorCut.Domain.Tests;

public sealed class TrimClipCommandTests
{
    [Fact]
    public void trim_duration_updates_item()
    {
        var (seq, trackId, itemId) = MakeOneClip();

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewDuration = MediaTime.FromSeconds(3),
        };

        Assert.True(cmd.Execute(seq).Success);
        Assert.Equal(3, seq.Tracks[0].Items[0].Duration.Seconds, 3);
    }

    [Fact]
    public void trim_zero_duration_returns_error()
    {
        var (seq, trackId, itemId) = MakeOneClip();

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewDuration = MediaTime.Zero,
        };

        Assert.False(cmd.Execute(seq).Success);
    }

    [Fact]
    public void trim_negative_start_returns_error()
    {
        var (seq, trackId, itemId) = MakeOneClip();

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewStart = MediaTime.FromSeconds(-1),
        };

        Assert.False(cmd.Execute(seq).Success);
    }

    [Fact]
    public void trim_undo_restores_values()
    {
        var (seq, trackId, itemId) = MakeOneClip();

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewDuration = MediaTime.FromSeconds(3),
        };

        cmd.Execute(seq);
        cmd.Undo(seq);

        Assert.Equal(10, seq.Tracks[0].Items[0].Duration.Seconds, 3);
    }

    private static (Sequence seq, TrackId trackId, TimelineItemId itemId) MakeOneClip()
    {
        var seq = new Sequence();
        var track = new Track { Kind = TrackKind.Video, Name = "V1" };
        var item = new TimelineItem { Duration = MediaTime.FromSeconds(10) };
        track.Items.Add(item);
        seq.Tracks.Add(track);
        return (seq, track.Id, item.Id);
    }
}
