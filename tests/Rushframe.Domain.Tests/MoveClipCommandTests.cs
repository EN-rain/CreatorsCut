namespace Rushframe.Domain.Tests;

public sealed class MoveClipCommandTests
{
    [Fact]
    public void move_between_tracks_validates_compatibility()
    {
        var seq = new Sequence();
        var videoTrack = new Track { Kind = TrackKind.Video, Name = "V1" };
        var audioTrack = new Track { Kind = TrackKind.Audio, Name = "A1" };
        var item = new TimelineItem { Kind = ItemKind.Text, Duration = MediaTime.FromSeconds(5) };
        videoTrack.Items.Add(item);
        seq.Tracks.Add(videoTrack);
        seq.Tracks.Add(audioTrack);

        var cmd = new Editing.MoveClipCommand
        {
            ItemId = item.Id,
            TargetTrackId = audioTrack.Id,
        };

        var result = cmd.Execute(seq);
        Assert.False(result.Success);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Single(seq.Tracks[0].Items);
        Assert.Empty(seq.Tracks[1].Items);
    }

    [Fact]
    public void move_video_to_audio_track_is_incompatible()
    {
        var seq = new Sequence();
        var videoTrack = new Track { Kind = TrackKind.Video, Name = "V1" };
        var audioTrack = new Track { Kind = TrackKind.Audio, Name = "A1" };
        var item = new TimelineItem { Duration = MediaTime.FromSeconds(5) };
        videoTrack.Items.Add(item);
        seq.Tracks.Add(videoTrack);
        seq.Tracks.Add(audioTrack);

        var cmd = new Editing.MoveClipCommand
        {
            ItemId = item.Id,
            TargetTrackId = audioTrack.Id,
        };

        var result = cmd.Execute(seq);
        Assert.True(result.Success);
    }

    [Fact]
    public void move_text_to_overlay_track_is_compatible()
    {
        var seq = new Sequence();
        var textTrack = new Track { Kind = TrackKind.Text, Name = "T1" };
        var overlayTrack = new Track { Kind = TrackKind.Overlay, Name = "O1" };
        var item = new TimelineItem { Kind = ItemKind.Text, Duration = MediaTime.FromSeconds(5) };
        textTrack.Items.Add(item);
        seq.Tracks.Add(textTrack);
        seq.Tracks.Add(overlayTrack);

        var cmd = new Editing.MoveClipCommand
        {
            ItemId = item.Id,
            TargetTrackId = overlayTrack.Id,
        };

        var result = cmd.Execute(seq);
        Assert.True(result.Success);
        Assert.Empty(textTrack.Items);
        Assert.Single(overlayTrack.Items);
    }

    [Fact]
    public void move_undo_restores_original_position()
    {
        var seq = new Sequence();
        var track1 = new Track { Kind = TrackKind.Video, Name = "V1" };
        var track2 = new Track { Kind = TrackKind.Overlay, Name = "O1" };
        var item = new TimelineItem { Kind = ItemKind.Text, Duration = MediaTime.FromSeconds(5) };
        track1.Items.Add(item);
        seq.Tracks.Add(track1);
        seq.Tracks.Add(track2);

        var cmd = new Editing.MoveClipCommand
        {
            ItemId = item.Id,
            TargetTrackId = track2.Id,
        };

        cmd.Execute(seq);
        cmd.Undo(seq);

        Assert.Single(track1.Items);
        Assert.Empty(track2.Items);
        Assert.Equal(item.Id, track1.Items[0].Id);
    }
}
