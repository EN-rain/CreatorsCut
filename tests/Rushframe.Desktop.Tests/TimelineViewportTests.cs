using Rushframe.Desktop.Viewport;
using Rushframe.Domain;

namespace Rushframe.Desktop.Tests;

public sealed class TimelineViewportTests
{
    [Fact]
    public void time_zero_maps_to_track_header_edge()
    {
        var viewport = new TimelineViewport
        {
            TrackHeaderWidth = 160,
            HorizontalOffset = 0,
        };

        Assert.Equal(160, viewport.TimeToPixel(MediaTime.Zero), 3);
        Assert.Equal(0, viewport.PixelToTime(160).Seconds, 3);
    }

    [Fact]
    public void time_and_pixel_conversion_round_trip()
    {
        var viewport = new TimelineViewport
        {
            TrackHeaderWidth = 160,
            HorizontalOffset = 275,
        };
        var time = MediaTime.FromSeconds(8.25);

        var pixel = viewport.TimeToPixel(time);
        var roundTrip = viewport.PixelToTime(pixel);

        Assert.Equal(time.Seconds, roundTrip.Seconds, 6);
    }

    [Fact]
    public void zoom_keeps_anchor_time_stable()
    {
        var viewport = new TimelineViewport
        {
            ViewportWidth = 1000,
            TrackHeaderWidth = 160,
            SequenceDurationSeconds = 120,
            HorizontalOffset = 300,
        };
        const double anchorPixel = 520;
        var anchorTimeBefore = viewport.PixelToTime(anchorPixel).Seconds;

        viewport.Zoom(1.75, anchorPixel);

        Assert.Equal(anchorTimeBefore, viewport.PixelToTime(anchorPixel).Seconds, 6);
    }

    [Fact]
    public void pan_is_clamped_to_content_bounds()
    {
        var viewport = new TimelineViewport
        {
            ViewportWidth = 1000,
            ViewportHeight = 300,
            TrackHeaderWidth = 160,
            SequenceDurationSeconds = 10,
            TrackCount = 3,
        };

        viewport.Pan(10_000, 10_000);
        Assert.Equal(0, viewport.HorizontalOffset, 3);
        Assert.Equal(0, viewport.VerticalOffset, 3);

        viewport.Pan(-10_000, -10_000);
        Assert.Equal(viewport.GetMaxHorizontalOffset(), viewport.HorizontalOffset, 3);
        Assert.Equal(viewport.GetMaxVerticalOffset(), viewport.VerticalOffset, 3);
    }
}
