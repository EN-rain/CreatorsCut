using CreatorCut.Domain;

namespace CreatorCut.Desktop.Viewport;

public sealed class TimelineViewport
{
    private const double DefaultPixelsPerSecond = 100.0;
    private const double MinZoom = 10.0;
    private const double MaxZoom = 1000.0;

    public double PixelsPerSecond { get; private set; } = DefaultPixelsPerSecond;
    public double HorizontalOffset { get; set; }
    public double VerticalOffset { get; set; }
    public double ViewportWidth { get; set; } = 800;
    public double ViewportHeight { get; set; } = 400;
    public double TrackHeaderWidth { get; set; } = 160;
    public double TrackHeight { get; set; } = 60;
    public double RulerHeight { get; set; } = 28;

    public double SequenceDurationSeconds { get; set; } = 60;
    public double TotalHeight => RulerHeight + 8 * TrackHeight;

    public double VisibleDurationSeconds => ViewportWidth / PixelsPerSecond;
    public double TimeAtLeftEdgeSeconds => HorizontalOffset / PixelsPerSecond;

    public double TimeToPixel(MediaTime time) =>
        time.Seconds * PixelsPerSecond - HorizontalOffset;

    public MediaTime PixelToTime(double pixelX)
    {
        var seconds = (pixelX + HorizontalOffset) / PixelsPerSecond;
        return MediaTime.FromSeconds(Math.Max(0, seconds));
    }

    public double TrackIndexToY(int trackIndex) =>
        RulerHeight + trackIndex * TrackHeight - VerticalOffset;

    public int YToTrackIndex(double pixelY) =>
        (int)((pixelY + VerticalOffset - RulerHeight) / TrackHeight);

    public void Zoom(double factor, double anchorPixelX)
    {
        var anchorTime = PixelToTime(anchorPixelX).Seconds;
        PixelsPerSecond = Math.Clamp(PixelsPerSecond * factor, MinZoom, MaxZoom);
        HorizontalOffset = anchorTime * PixelsPerSecond - anchorPixelX;
    }

    public void Pan(double deltaX, double deltaY)
    {
        HorizontalOffset = Math.Max(0, HorizontalOffset - deltaX);
        VerticalOffset = Math.Max(0, VerticalOffset - deltaY);
    }

    public double GetClipX(MediaTime timelineStart) => TimeToPixel(timelineStart);
    public double GetClipWidth(MediaTime duration) => duration.Seconds * PixelsPerSecond;

    public bool IsInViewport(double pixelX, double pixelY) =>
        pixelX >= -TrackHeaderWidth && pixelX < ViewportWidth &&
        pixelY >= RulerHeight && pixelY < ViewportHeight;

    public double GetMaxHorizontalOffset() =>
        Math.Max(0, SequenceDurationSeconds * PixelsPerSecond - ViewportWidth);
}
