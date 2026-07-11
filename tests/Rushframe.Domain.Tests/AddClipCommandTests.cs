namespace Rushframe.Domain.Tests;

public sealed class AddClipCommandTests
{
    [Fact]
    public void add_clip_to_track()
    {
        var (seq, trackId) = MakeSequence();

        var cmd = new Editing.AddClipCommand
        {
            TrackId = trackId,
            Item = new TimelineItem { Duration = MediaTime.FromSeconds(5) },
        };

        var result = cmd.Execute(seq);
        Assert.True(result.Success);
        Assert.Single(seq.Tracks[0].Items);
        Assert.Equal(5, seq.Tracks[0].Items[0].Duration.Seconds, 3);
    }

    [Fact]
    public void add_clip_undo_removes_it()
    {
        var (seq, trackId) = MakeSequence();

        var cmd = new Editing.AddClipCommand
        {
            TrackId = trackId,
            Item = new TimelineItem { Duration = MediaTime.FromSeconds(5) },
        };

        cmd.Execute(seq);
        var undoResult = cmd.Undo(seq);
        Assert.True(undoResult.Success);
        Assert.Empty(seq.Tracks[0].Items);
    }

    [Fact]
    public void add_to_nonexistent_track_returns_error()
    {
        var (seq, _) = MakeSequence();
        var fakeId = TrackId.New();

        var cmd = new Editing.AddClipCommand
        {
            TrackId = fakeId,
            Item = new TimelineItem { Duration = MediaTime.FromSeconds(5) },
        };

        var result = cmd.Execute(seq);
        Assert.False(result.Success);
        Assert.IsType<TrackNotFoundError>(result.Error);
    }

    private static (Sequence seq, TrackId trackId) MakeSequence()
    {
        var seq = new Sequence();
        var track = new Track { Kind = TrackKind.Video, Name = "V1" };
        seq.Tracks.Add(track);
        return (seq, track.Id);
    }
}
