namespace Rushframe.Domain.Tests;

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
    public void trim_source_start_updates_and_undo_restores()
    {
        var (seq, trackId, itemId) = MakeOneClip();
        var item = seq.Tracks[0].Items[0];
        item.SourceStart = MediaTime.FromSeconds(2);

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewStart = MediaTime.FromSeconds(1),
            NewDuration = MediaTime.FromSeconds(9),
            NewSourceStart = MediaTime.FromSeconds(3),
        };

        Assert.True(cmd.Execute(seq).Success);
        Assert.Equal(3, item.SourceStart.Seconds, 3);

        Assert.True(cmd.Undo(seq).Success);
        Assert.Equal(0, item.TimelineStart.Seconds, 3);
        Assert.Equal(10, item.Duration.Seconds, 3);
        Assert.Equal(2, item.SourceStart.Seconds, 3);
    }

    [Fact]
    public void invalid_duration_does_not_partially_apply_other_values()
    {
        var (seq, trackId, itemId) = MakeOneClip();
        var item = seq.Tracks[0].Items[0];
        item.SourceStart = MediaTime.FromSeconds(2);

        var cmd = new Editing.TrimClipCommand
        {
            TrackId = trackId,
            ItemId = itemId,
            NewStart = MediaTime.FromSeconds(5),
            NewSourceStart = MediaTime.FromSeconds(7),
            NewDuration = MediaTime.Zero,
        };

        Assert.False(cmd.Execute(seq).Success);
        Assert.Equal(0, item.TimelineStart.Seconds, 3);
        Assert.Equal(10, item.Duration.Seconds, 3);
        Assert.Equal(2, item.SourceStart.Seconds, 3);
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
