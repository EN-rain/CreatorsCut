using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CreatorCut.Desktop.Viewport;
using CreatorCut.Domain;

namespace CreatorCut.Desktop.Timeline;

public sealed class TimelineControl : FrameworkElement
{
    private readonly TimelineViewport _viewport = new();

    public Sequence? Sequence
    {
        get => _sequence;
        set { _sequence = value; InvalidateVisual(); }
    }

    private Sequence? _sequence;
    private Point _lastMousePos;
    private bool _isPanning;
    private bool _isDraggingClip;
    private bool _isTrimming;
    private TimelineItem? _dragItem;
    private int _dragTrackIndex;
    private MediaTime _dragStartTime;
    private MediaTime _dragOrigDuration;
    private MediaTime _playheadTime;
    private TimelineItem? _selectedItem;
    private int _selectedTrackIndex;
    private bool _dragFromPlayhead;

    private enum DragMode { None, Move, TrimLeft, TrimRight }

    public MediaTime PlayheadTime { get => _playheadTime; set { _playheadTime = value; InvalidateVisual(); } }
    public TimelineItem? SelectedItem => _selectedItem;
    public int SelectedTrackIndex => _selectedTrackIndex;

    public event EventHandler<TimelineItem>? ClipSelected;
    public event EventHandler? PlayheadMoved;

    private const double TrimEdgeThreshold = 8;

    static TimelineControl()
    {
        FocusableProperty.OverrideMetadata(typeof(TimelineControl), new FrameworkPropertyMetadata(true));
    }

    public TimelineControl()
    {
        ClipToBounds = true;
        SnapsToDevicePixels = true;
    }

    public void ScrollToTime(MediaTime time)
    {
        var targetPixel = _viewport.TimeToPixel(time);
        if (targetPixel < _viewport.TrackHeaderWidth || targetPixel > _viewport.ViewportWidth)
        {
            _viewport.HorizontalOffset = time.Seconds * _viewport.PixelsPerSecond - _viewport.ViewportWidth / 2;
            _viewport.HorizontalOffset = Math.Max(0, _viewport.HorizontalOffset);
            InvalidateVisual();
        }
    }

    public void ScrollBy(double deltaPixels)
    {
        _viewport.HorizontalOffset = Math.Max(0, _viewport.HorizontalOffset + deltaPixels);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (Sequence == null) return;
        UpdateSequenceDuration();

        DrawBackground(dc);
        DrawRuler(dc);
        DrawTrackHeaders(dc);

        for (int i = 0; i < Sequence.Tracks.Count; i++)
            DrawTrack(dc, Sequence.Tracks[i], i);

        DrawPlayhead(dc);
    }

    private void UpdateSequenceDuration()
    {
        double max = 0;
        if (Sequence != null)
        {
            foreach (var track in Sequence.Tracks)
            foreach (var item in track.Items)
            {
                var end = item.TimelineStart.Seconds + item.Duration.Seconds;
                if (end > max) max = end;
            }
        }
        _viewport.SequenceDurationSeconds = Math.Max(max + 10, 60);
    }

    private void DrawBackground(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.White, null, new Rect(RenderSize));
    }

    private void DrawRuler(DrawingContext dc)
    {
        var rect = new Rect(_viewport.TrackHeaderWidth, 0,
            RenderSize.Width - _viewport.TrackHeaderWidth, _viewport.RulerHeight);
        dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.Gray, 0.5), rect);

        var step = Math.Max(1, (int)(50 / _viewport.PixelsPerSecond));
        var startSec = (int)_viewport.TimeAtLeftEdgeSeconds;
        startSec = startSec / step * step;

        for (var s = startSec; s <= _viewport.TimeAtLeftEdgeSeconds + _viewport.VisibleDurationSeconds + 1; s += step)
        {
            var x = _viewport.TimeToPixel(MediaTime.FromSeconds(s));
            if (x < _viewport.TrackHeaderWidth || x > RenderSize.Width) continue;

            var ft = new FormattedText($"{s}s",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, Brushes.Black, 1.25);
            dc.DrawText(ft, new Point(x + 3, 2));
            dc.DrawLine(new Pen(Brushes.Gray, 0.5), new Point(x, 6), new Point(x, _viewport.RulerHeight));
        }
    }

    private void DrawTrackHeaders(DrawingContext dc)
    {
        for (int i = 0; i < (Sequence?.Tracks.Count ?? 0); i++)
        {
            var track = Sequence!.Tracks[i];
            var y = _viewport.TrackIndexToY(i);
            var rect = new Rect(0, y, _viewport.TrackHeaderWidth, _viewport.TrackHeight);
            dc.DrawRectangle(Brushes.LightYellow, new Pen(Brushes.Gray, 0.5), rect);

            if (track.Locked)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)), null, rect);

            var label = string.IsNullOrEmpty(track.Name) ? $"Track {i + 1}" : track.Name;
            if (track.Muted) label = "[MUTED] " + label;
            if (track.Locked) label = "🔒 " + label;
            var ft = new FormattedText(label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.Black, 1.25);
            dc.DrawText(ft, new Point(4, y + 20));

            if (track.Solo)
            {
                var soloFt = new FormattedText("S",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, Brushes.DarkOrange, 1.25);
                dc.DrawText(soloFt, new Point(4, y + 4));
            }
        }
    }

    private void DrawTrack(DrawingContext dc, Track track, int trackIndex)
    {
        var yBase = _viewport.TrackIndexToY(trackIndex);
        var clipRect = new Rect(_viewport.TrackHeaderWidth, yBase,
            RenderSize.Width - _viewport.TrackHeaderWidth, _viewport.TrackHeight);

        if (trackIndex % 2 == 0)
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(8, 0, 0, 0)), null, clipRect);

        dc.DrawRectangle(null, new Pen(Brushes.LightGray, 0.5), clipRect);

        foreach (var item in track.Items)
        {
            var x = _viewport.GetClipX(item.TimelineStart);
            var w = _viewport.GetClipWidth(item.Duration);
            if (x + w < _viewport.TrackHeaderWidth || x > RenderSize.Width) continue;

            var rect = new Rect(x, yBase + 2, w, _viewport.TrackHeight - 4);
            var isSelected = _selectedItem != null && _selectedItem.Id == item.Id;

            var color = item.Kind switch
            {
                ItemKind.Clip => Brushes.CornflowerBlue,
                ItemKind.Text => Brushes.Orange,
                ItemKind.Image => Brushes.MediumSeaGreen,
                ItemKind.Sticker => Brushes.Gold,
                ItemKind.AdjustmentLayer => Brushes.Plum,
                _ => Brushes.Gray,
            };
            var borderPen = isSelected ? new Pen(Brushes.Red, 2) : new Pen(Brushes.DarkGray, 0.5);
            dc.DrawRectangle(color, borderPen, rect);

            if (w > 40)
            {
                var label = item.Kind.ToString();
                var ft = new FormattedText(label,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, Brushes.White, 1.25);
                dc.DrawText(ft, new Point(x + 3, yBase + 8));

                if (item.FadeInDuration.Seconds > 0)
                {
                    var fw = _viewport.GetClipWidth(item.FadeInDuration);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), null,
                        new Rect(x, yBase + 2, Math.Min(fw, w), 6));
                }
                if (item.FadeOutDuration.Seconds > 0)
                {
                    var fw = _viewport.GetClipWidth(item.FadeOutDuration);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), null,
                        new Rect(x + w - Math.Min(fw, w), yBase + 2, Math.Min(fw, w), 6));
                }
            }
        }
    }

    private void DrawPlayhead(DrawingContext dc)
    {
        var x = _viewport.TimeToPixel(_playheadTime);
        if (x < _viewport.TrackHeaderWidth || x > RenderSize.Width) return;

        dc.DrawLine(new Pen(Brushes.Red, 1.5), new Point(x, 0), new Point(x, RenderSize.Height));

        var ft = new FormattedText($"{_playheadTime.Seconds:F1}s",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, Brushes.Red, 1.25);
        dc.DrawText(ft, new Point(x + 3, 1));
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        _viewport.Zoom(factor, e.GetPosition(this).X);
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus();
        _lastMousePos = e.GetPosition(this);
        CaptureMouse();

        if (e.ChangedButton == MouseButton.Middle)
        {
            _isPanning = true;
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            var pos = e.GetPosition(this);

            if (pos.Y < _viewport.RulerHeight && pos.X > _viewport.TrackHeaderWidth)
            {
                _playheadTime = _viewport.PixelToTime(pos.X);
                _dragFromPlayhead = true;
                ClipSelected?.Invoke(this, null!);
                PlayheadMoved?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            var (item, trackIdx, itemIdx) = HitTest(pos);
            if (item != null)
            {
                _selectedItem = item;
                _selectedTrackIndex = trackIdx;
                _dragTrackIndex = trackIdx;

                var clipX = _viewport.GetClipX(item.TimelineStart);
                var clipW = _viewport.GetClipWidth(item.Duration);
                var distFromLeft = pos.X - clipX;
                var distFromRight = clipX + clipW - pos.X;

                if (distFromLeft < TrimEdgeThreshold && item.Duration.Seconds > 1)
                {
                    _isTrimming = true;
                    _dragItem = item;
                    _dragStartTime = item.TimelineStart;
                    _dragOrigDuration = item.Duration;
                    Cursor = Cursors.SizeWE;
                }
                else if (distFromRight < TrimEdgeThreshold && item.Duration.Seconds > 1)
                {
                    _isTrimming = true;
                    _dragItem = item;
                    _dragStartTime = item.TimelineStart;
                    _dragOrigDuration = item.Duration;
                    Cursor = Cursors.SizeWE;
                }
                else
                {
                    _isDraggingClip = true;
                    _dragItem = item;
                    _dragStartTime = item.TimelineStart;
                    Cursor = Cursors.SizeAll;
                }

                ClipSelected?.Invoke(this, item);
                InvalidateVisual();
                e.Handled = true;
            }
            else
            {
                _selectedItem = null;
                InvalidateVisual();

                _playheadTime = _viewport.PixelToTime(pos.X);
                PlayheadMoved?.Invoke(this, EventArgs.Empty);
                InvalidateVisual();
                e.Handled = true;
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_isPanning)
        {
            _viewport.Pan(pos.X - _lastMousePos.X, pos.Y - _lastMousePos.Y);
            _lastMousePos = pos;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_dragFromPlayhead)
        {
            _playheadTime = _viewport.PixelToTime(pos.X);
            PlayheadMoved?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isTrimming && _dragItem != null)
        {
            var deltaSec = (pos.X - _lastMousePos.X) / _viewport.PixelsPerSecond;
            var newDurationSec = _dragOrigDuration.Seconds + deltaSec;
            if (newDurationSec >= 0.5)
            {
                _dragItem.Duration = MediaTime.FromSeconds(newDurationSec);
                if (pos.X < _viewport.GetClipX(_dragItem.TimelineStart))
                {
                    var trimLeft = (_viewport.GetClipX(_dragItem.TimelineStart) - pos.X) / _viewport.PixelsPerSecond;
                    _dragItem.TimelineStart = MediaTime.FromSeconds(Math.Max(0, _dragStartTime.Seconds - trimLeft));
                    _dragItem.Duration = MediaTime.FromSeconds(newDurationSec + trimLeft);
                }
            }
            _lastMousePos = pos;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isDraggingClip && _dragItem != null)
        {
            var deltaSec = (pos.X - _lastMousePos.X) / _viewport.PixelsPerSecond;
            var newStart = MediaTime.FromSeconds(
                Math.Max(0, _dragStartTime.Seconds + deltaSec));
            _dragItem.TimelineStart = newStart;
            _lastMousePos = pos;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var (hoverItem, _, _) = HitTest(pos);
        if (hoverItem != null)
        {
            var clipX = _viewport.GetClipX(hoverItem.TimelineStart);
            var clipW = _viewport.GetClipWidth(hoverItem.Duration);
            var distFromLeft = pos.X - clipX;
            var distFromRight = clipX + clipW - pos.X;
            Cursor = (distFromLeft < TrimEdgeThreshold || distFromRight < TrimEdgeThreshold)
                ? Cursors.SizeWE : Cursors.Arrow;
        }
        else
        {
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        Cursor = Cursors.Arrow;

        if (_isPanning) { _isPanning = false; e.Handled = true; }
        if (_isDraggingClip) { _isDraggingClip = false; e.Handled = true; }
        if (_isTrimming) { _isTrimming = false; e.Handled = true; }
        if (_dragFromPlayhead) { _dragFromPlayhead = false; e.Handled = true; }

        _dragItem = null;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            e.Handled = true;
        }
        if (e.Key == Key.Delete && _selectedItem != null)
        {
            ClipSelected?.Invoke(this, null!);
            e.Handled = true;
        }
    }

    private (TimelineItem? item, int trackIndex, int itemIndex) HitTest(Point pos)
    {
        if (Sequence == null) return (null, -1, -1);

        for (int t = 0; t < Sequence.Tracks.Count; t++)
        {
            var track = Sequence.Tracks[t];
            var y = _viewport.TrackIndexToY(t);
            if (pos.Y < y || pos.Y > y + _viewport.TrackHeight) continue;

            for (int i = 0; i < track.Items.Count; i++)
            {
                var item = track.Items[i];
                var x = _viewport.GetClipX(item.TimelineStart);
                var w = _viewport.GetClipWidth(item.Duration);
                if (pos.X >= x && pos.X <= x + w)
                    return (item, t, i);
            }
        }

        return (null, -1, -1);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _viewport.ViewportWidth = availableSize.Width;
        _viewport.ViewportHeight = availableSize.Height;
        return availableSize;
    }
}
