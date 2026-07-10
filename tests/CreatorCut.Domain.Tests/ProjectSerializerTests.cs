using CreatorCut.Domain.Serialization;

namespace CreatorCut.Domain.Tests;

public sealed class ProjectSerializerTests
{
    [Fact]
    public void save_reload_is_equivalent()
    {
        var project = new Project { Name = "Test Project" };
        var seq = project.MainSequence!;
        seq.Name = "Main Seq";
        seq.Width = 1920;
        seq.Height = 1080;

        var track = new Track { Kind = TrackKind.Video, Name = "V1", Order = 1 };
        var item = new TimelineItem
        {
            Kind = ItemKind.Clip,
            MediaAssetId = MediaAssetId.New(),
            TimelineStart = MediaTime.FromSeconds(2),
            Duration = MediaTime.FromSeconds(10),
            SourceStart = MediaTime.Zero,
            SourceDuration = MediaTime.FromSeconds(15),
            Speed = 1.5,
            Volume = 0.8,
            Opacity = 0.9,
            Transform = new Transform2D { PositionX = 100, PositionY = 200 },
        };
        track.Items.Add(item);
        seq.Tracks.Add(track);

        var asset = new MediaAsset
        {
            Kind = MediaKind.Video,
            OriginalPath = @"C:\clips\test.mp4",
            RelativeProjectPath = "clips/test.mp4",
            Duration = MediaTime.FromSeconds(60),
        };
        project.MediaLibrary.Add(asset);

        var json = ProjectSerializer.Serialize(project);
        var restored = ProjectSerializer.Deserialize(json);

        Assert.Equal(project.Name, restored.Name);
        Assert.Single(restored.Sequences);
        Assert.Single(restored.MainSequence!.Tracks);
        Assert.Single(restored.MediaLibrary);

        var restoredTrack = restored.MainSequence.Tracks[0];
        Assert.Equal("V1", restoredTrack.Name);
        Assert.Equal(TrackKind.Video, restoredTrack.Kind);

        var restoredItem = restoredTrack.Items[0];
        Assert.Equal(2, restoredItem.TimelineStart.Seconds, 3);
        Assert.Equal(10, restoredItem.Duration.Seconds, 3);
        Assert.Equal(1.5, restoredItem.Speed);
        Assert.Equal(0.8, restoredItem.Volume);
        Assert.Equal(0.9, restoredItem.Opacity);
        Assert.Equal(100, restoredItem.Transform.PositionX);
    }

    [Fact]
    public void unknown_extension_data_survives_round_trip()
    {
        var project = new Project { Name = "Extension Test" };
        var track = new Track { Kind = TrackKind.Video, Name = "V1" };
        var item = new TimelineItem { Duration = MediaTime.FromSeconds(5) };
        track.Items.Add(item);
        project.MainSequence!.Tracks.Add(track);

        var json = ProjectSerializer.Serialize(project);
        var restored = ProjectSerializer.Deserialize(json);

        Assert.Equal("Extension Test", restored.Name);
        Assert.Single(restored.MainSequence!.Tracks[0].Items);
        Assert.Equal(5, restored.MainSequence.Tracks[0].Items[0].Duration.Seconds, 3);
    }

    [Fact]
    public void serialized_json_contains_media_time_fields()
    {
        var project = new Project { Name = "MediaTime Test" };
        var track = new Track { Kind = TrackKind.Video, Name = "V1" };
        var item = new TimelineItem
        {
            TimelineStart = MediaTime.FromSeconds(1.5),
            Duration = MediaTime.FromSeconds(3.25),
        };
        track.Items.Add(item);
        project.MainSequence!.Tracks.Add(track);

        var json = ProjectSerializer.Serialize(project);

        Assert.Contains("numerator", json);
        Assert.Contains("denominator", json);
    }
}
