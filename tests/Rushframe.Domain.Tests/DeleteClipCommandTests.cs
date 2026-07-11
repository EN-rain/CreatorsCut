namespace Rushframe.Domain.Tests;

public sealed class DeleteClipCommandTests
{
    [Fact]
    public void delete_removes_item()
    {
        var (seq, trackId, itemId) = MakeClip();

        var cmd = new Editing.DeleteClipCommand { ItemId = itemId };
        Assert.True(cmd.Execute(seq).Success);
        Assert.Empty(seq.Tracks[0].Items);
    }

    [Fact]
    public void delete_undo_restores_item()
    {
        var (seq, trackId, itemId) = MakeClip();

        var cmd = new Editing.DeleteClipCommand { ItemId = itemId };
        cmd.Execute(seq);
        cmd.Undo(seq);

        Assert.Single(seq.Tracks[0].Items);
        Assert.Equal(itemId, seq.Tracks[0].Items[0].Id);
    }

    private static (Sequence seq, TrackId trackId, TimelineItemId itemId) MakeClip()
    {
        var seq = new Sequence();
        var track = new Track { Kind = TrackKind.Video };
        var item = new TimelineItem { Duration = MediaTime.FromSeconds(5) };
        track.Items.Add(item);
        seq.Tracks.Add(track);
        return (seq, track.Id, item.Id);
    }
}
