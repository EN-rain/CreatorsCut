using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rushframe.Desktop.Viewport;
using Rushframe.Domain;

namespace Rushframe.Desktop.Timeline;

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
    private MediaTime _dragOrigSourceStart;
    private double _dragOriginMouseX;
    private DragMode _dragMode;
    private MediaTime _playheadTime;
    private TimelineItem? _selectedItem;
    private int _selectedTrackIndex;
    private bool _dragFromPlayhead;

    private enum DragMode { None, Move, TrimLeft, TrimRight }

    public MediaTime PlayheadTime { get => _playheadTime; set { _playheadTime = value; InvalidateVisual(); } }
    public TimelineItem? SelectedItem => _selectedItem;
    public int SelectedTrackIndex => _selectedTrackIndex;
    public bool SnapEnabled { get; set; } = true;

    public event EventHandler<TimelineItem?>? ClipSelected;
    public event EventHandler? PlayheadMoved;
    public event EventHandler? DeleteSelectedClipRequested;
    public event EventHandler<ClipMoveRequestedEventArgs>? ClipMoveRequested;
    public event EventHandler<ClipTrimRequestedEventArgs>? ClipTrimRequested;

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
            var contentWidth = Math.Max(0, _viewport.ViewportWidth - _viewport.TrackHeaderWidth);
            _viewport.HorizontalOffset = Math.Clamp(
                time.Seconds * _viewport.PixelsPerSecond - contentWidth / 2,
                0,
                _viewport.GetMaxHorizontalOffset());
            InvalidateVisual();
        }
    }

    public void ScrollBy(double deltaPixels)
    {
        _viewport.HorizontalOffset = Math.Clamp(
            _viewport.HorizontalOffset + deltaPixels,
            0,
            _viewport.GetMaxHorizontalOffset());
        InvalidateVisual();
    }

    public void SetZoomScale(double scale)
    {
        _viewport.SetZoomScale(scale, Math.Max(_viewport.TrackHeaderWidth, RenderSize.Width / 2));
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        DrawBackground(dc);
        if (Sequence == null) return;
        UpdateSequenceDuration();

        DrawRuler(dc);
        DrawTrackHeaders(dc);

        for (int i = 0; i < Sequence.Tracks.Count; i++)
            DrawTrack(dc, Sequence.Tracks[i], i);

        DrawMarkers(dc);
        DrawDraggedClipGhost(dc);
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
        _viewport.TrackCount = Sequence?.Tracks.Count ?? 0;
        _viewport.HorizontalOffset = Math.Clamp(_viewport.HorizontalOffset, 0, _viewport.GetMaxHorizontalOffset());
        _viewport.VerticalOffset = Math.Clamp(_viewport.VerticalOffset, 0, _viewport.GetMaxVerticalOffset());
    }

    private void DrawBackground(DrawingContext dc)
    {
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(17, 22, 27)), null, new Rect(RenderSize));
    }

    private void DrawRuler(DrawingContext dc)
    {
        var rect = new Rect(_viewport.TrackHeaderWidth, 0,
            RenderSize.Width - _viewport.TrackHeaderWidth, _viewport.RulerHeight);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(21, 26, 31)), new Pen(new SolidColorBrush(Color.FromRgb(43, 51, 59)), 0.5), rect);

        var step = Math.Max(1, (int)(50 / _viewport.PixelsPerSecond));
        var startSec = (int)_viewport.TimeAtLeftEdgeSeconds;
        startSec = startSec / step * step;

        for (var s = startSec; s <= _viewport.TimeAtLeftEdgeSeconds + _viewport.VisibleDurationSeconds + 1; s += step)
        {
            var x = _viewport.TimeToPixel(MediaTime.FromSeconds(s));
            if (x < _viewport.TrackHeaderWidth || x > RenderSize.Width) continue;

            var labelWouldCollideWithPlayhead = Math.Abs(s - _playheadTime.Seconds) < 0.05;
            if (!labelWouldCollideWithPlayhead)
            {
                var ft = new FormattedText($"{s}s",
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, new SolidColorBrush(Color.FromRgb(142, 154, 165)), 1.25);
                dc.DrawText(ft, new Point(x + 3, 2));
            }
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(58, 68, 78)), 0.5), new Point(x, 6), new Point(x, _viewport.RulerHeight));
        }
    }

    private void DrawTrackHeaders(DrawingContext dc)
    {
        for (int i = 0; i < (Sequence?.Tracks.Count ?? 0); i++)
        {
            var track = Sequence!.Tracks[i];
            var y = _viewport.TrackIndexToY(i);
            var rect = new Rect(0, y, _viewport.TrackHeaderWidth, _viewport.TrackHeight);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(24, 30, 36)), new Pen(new SolidColorBrush(Color.FromRgb(43, 51, 59)), 0.5), rect);

            if (track.Locked)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)), null, rect);

            var label = string.IsNullOrEmpty(track.Name) ? $"Track {i + 1}" : track.Name;
            if (track.Muted) label = "[MUTED] " + label;
            if (track.Locked) label = "🔒 " + label;
            var ft = new FormattedText(label,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, new SolidColorBrush(Color.FromRgb(231, 235, 239)), 1.25);
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
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(22, 255, 255, 255)), null, clipRect);

        dc.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(43, 51, 59)), 0.5), clipRect);

        foreach (var item in track.Items)
        {
            if (_isDraggingClip && _dragItem?.Id == item.Id && trackIndex != _dragTrackIndex)
                continue;

            var x = _viewport.GetClipX(item.TimelineStart);
            var w = _viewport.GetClipWidth(item.Duration);
            if (x + w < _viewport.TrackHeaderWidth || x > RenderSize.Width) continue;

            var rect = new Rect(x, yBase + 2, w, _viewport.TrackHeight - 4);
            var isSelected = _selectedItem != null && _selectedItem.Id == item.Id;

            var color = item.Kind switch
            {
                ItemKind.Clip => new SolidColorBrush(Color.FromRgb(49, 114, 163)),
                ItemKind.Text => new SolidColorBrush(Color.FromRgb(126, 75, 166)),
                ItemKind.Image => new SolidColorBrush(Color.FromRgb(69, 134, 105)),
                ItemKind.Sticker => new SolidColorBrush(Color.FromRgb(174, 132, 49)),
                ItemKind.AdjustmentLayer => new SolidColorBrush(Color.FromRgb(111, 87, 145)),
                _ => new SolidColorBrush(Color.FromRgb(84, 94, 104)),
            };
            var borderPen = isSelected
                ? new Pen(new SolidColorBrush(Color.FromRgb(45, 156, 255)), 2)
                : new Pen(new SolidColorBrush(Color.FromRgb(26, 31, 36)), 0.75);
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

    private void DrawMarkers(DrawingContext dc)
    {
        if (Sequence == null) return;

        var markerBrush = new SolidColorBrush(Color.FromRgb(242, 184, 75));
        var markerPen = new Pen(markerBrush, 1);
        foreach (var marker in Sequence.Markers)
        {
            var x = _viewport.TimeToPixel(marker.Time);
            if (x < _viewport.TrackHeaderWidth || x > RenderSize.Width) continue;

            dc.DrawLine(markerPen, new Point(x, _viewport.RulerHeight - 6), new Point(x, RenderSize.Height));
            var triangle = new StreamGeometry();
            using (var context = triangle.Open())
            {
                context.BeginFigure(new Point(x - 5, 0), true, true);
                context.LineTo(new Point(x + 5, 0), true, false);
                context.LineTo(new Point(x, 7), true, false);
            }
            triangle.Freeze();
            dc.DrawGeometry(markerBrush, null, triangle);

            if (!string.IsNullOrWhiteSpace(marker.Label))
            {
                var label = new FormattedText(
                    marker.Label,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    9,
                    markerBrush,
                    1.25);
                dc.DrawText(label, new Point(x + 5, 10));
            }
        }
    }

    private void DrawDraggedClipGhost(DrawingContext dc)
    {
        if (!_isDraggingClip || _dragItem == null || _dragTrackIndex == _selectedTrackIndex) return;
        if (Sequence == null || _dragTrackIndex < 0 || _dragTrackIndex >= Sequence.Tracks.Count) return;

        var x = _viewport.GetClipX(_dragItem.TimelineStart);
        var width = _viewport.GetClipWidth(_dragItem.Duration);
        var y = _viewport.TrackIndexToY(_dragTrackIndex) + 3;
        var rect = new Rect(x, y, width, _viewport.TrackHeight - 6);
        var fill = new SolidColorBrush(Color.FromArgb(135, 90, 140, 255));
        var border = new Pen(new SolidColorBrush(Color.FromRgb(115, 160, 255)), 2);
        dc.DrawRoundedRectangle(fill, border, rect, 3, 3);
    }

    private void DrawPlayhead(DrawingContext dc)
    {
        var x = _viewport.TimeToPixel(_playheadTime);
        if (x < _viewport.TrackHeaderWidth || x > RenderSize.Width) return;

        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(45, 156, 255)), 1.5), new Point(x, 0), new Point(x, RenderSize.Height));

        var ft = new FormattedText($"{_playheadTime.Seconds:F1}s",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9, new SolidColorBrush(Color.FromRgb(45, 156, 255)), 1.25);
        var labelX = Math.Min(Math.Max(x + 6, _viewport.TrackHeaderWidth + 4), Math.Max(_viewport.TrackHeaderWidth + 4, RenderSize.Width - ft.Width - 4));
        var labelRect = new Rect(labelX - 3, 2, ft.Width + 6, ft.Height + 3);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(14, 18, 24)), new Pen(new SolidColorBrush(Color.FromRgb(45, 156, 255)), 0.75), labelRect, 3, 3);
        dc.DrawText(ft, new Point(labelX, 3));
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
        var pos = e.GetPosition(this);
        _lastMousePos = pos;

        if (e.ChangedButton == MouseButton.Right)
        {
            var (contextItem, contextTrackIndex, _) = HitTest(pos);
            _selectedItem = contextItem;
            _selectedTrackIndex = contextTrackIndex;
            ClipSelected?.Invoke(this, contextItem);
            InvalidateVisual();
            return;
        }

        if (e.ChangedButton == MouseButton.Middle)
        {
            CaptureMouse();
            _isPanning = true;
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Left) return;

        CaptureMouse();
        if (pos.Y < _viewport.RulerHeight && pos.X > _viewport.TrackHeaderWidth)
        {
            _playheadTime = HitTestMarker(pos.X)?.Time ?? _viewport.PixelToTime(pos.X);
            _dragFromPlayhead = true;
            _selectedItem = null;
            _selectedTrackIndex = -1;
            ClipSelected?.Invoke(this, null);
            PlayheadMoved?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var (item, trackIdx, _) = HitTest(pos);
        if (item == null)
        {
            _selectedItem = null;
            _selectedTrackIndex = -1;
            ClipSelected?.Invoke(this, null);
            if (pos.X > _viewport.TrackHeaderWidth)
            {
                _playheadTime = _viewport.PixelToTime(pos.X);
                PlayheadMoved?.Invoke(this, EventArgs.Empty);
            }
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _selectedItem = item;
        _selectedTrackIndex = trackIdx;
        _dragTrackIndex = trackIdx;
        ClipSelected?.Invoke(this, item);

        var track = Sequence!.Tracks[trackIdx];
        if (track.Locked || item.Locked)
        {
            ReleaseMouseCapture();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _dragItem = item;
        _dragStartTime = item.TimelineStart;
        _dragOrigDuration = item.Duration;
        _dragOrigSourceStart = item.SourceStart;
        _dragOriginMouseX = pos.X;

        var clipX = _viewport.GetClipX(item.TimelineStart);
        var clipW = _viewport.GetClipWidth(item.Duration);
        var distFromLeft = pos.X - clipX;
        var distFromRight = clipX + clipW - pos.X;

        if (distFromLeft < TrimEdgeThreshold && item.Duration.Seconds > 0.2)
        {
            _isTrimming = true;
            _dragMode = DragMode.TrimLeft;
            Cursor = Cursors.SizeWE;
        }
        else if (distFromRight < TrimEdgeThreshold && item.Duration.Seconds > 0.2)
        {
            _isTrimming = true;
            _dragMode = DragMode.TrimRight;
            Cursor = Cursors.SizeWE;
        }
        else
        {
            _isDraggingClip = true;
            _dragMode = DragMode.Move;
            Cursor = Cursors.SizeAll;
        }

        InvalidateVisual();
        e.Handled = true;
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
            var totalDeltaSeconds = (pos.X - _dragOriginMouseX) / _viewport.PixelsPerSecond;
            const double minimumDurationSeconds = 0.1;

            if (_dragMode == DragMode.TrimLeft)
            {
                var candidateStartSeconds = Math.Clamp(
                    _dragStartTime.Seconds + totalDeltaSeconds,
                    0,
                    _dragStartTime.Seconds + _dragOrigDuration.Seconds - minimumDurationSeconds);
                var candidateStart = SnapTime(MediaTime.FromSeconds(candidateStartSeconds), _dragItem);
                candidateStartSeconds = Math.Clamp(
                    candidateStart.Seconds,
                    0,
                    _dragStartTime.Seconds + _dragOrigDuration.Seconds - minimumDurationSeconds);

                var actualDelta = candidateStartSeconds - _dragStartTime.Seconds;
                _dragItem.TimelineStart = MediaTime.FromSeconds(candidateStartSeconds);
                _dragItem.Duration = MediaTime.FromSeconds(_dragOrigDuration.Seconds - actualDelta);
                _dragItem.SourceStart = MediaTime.FromSeconds(
                    Math.Max(0, _dragOrigSourceStart.Seconds + actualDelta * Math.Max(0.1, _dragItem.Speed)));
            }
            else
            {
                var candidateEndSeconds = _dragStartTime.Seconds + _dragOrigDuration.Seconds + totalDeltaSeconds;
                var candidateEnd = SnapTime(MediaTime.FromSeconds(candidateEndSeconds), _dragItem);
                var newDurationSeconds = Math.Max(
                    minimumDurationSeconds,
                    candidateEnd.Seconds - _dragStartTime.Seconds);
                _dragItem.TimelineStart = _dragStartTime;
                _dragItem.Duration = MediaTime.FromSeconds(newDurationSeconds);
                _dragItem.SourceStart = _dragOrigSourceStart;
            }

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isDraggingClip && _dragItem != null)
        {
            var totalDeltaSeconds = (pos.X - _dragOriginMouseX) / _viewport.PixelsPerSecond;
            var candidateStart = MediaTime.FromSeconds(
                Math.Max(0, _dragStartTime.Seconds + totalDeltaSeconds));
            _dragItem.TimelineStart = SnapMoveStart(candidateStart, _dragItem.Duration, _dragItem);

            var candidateTrackIndex = GetTrackIndexAtY(pos.Y);
            if (candidateTrackIndex >= 0)
            {
                var candidateTrack = Sequence!.Tracks[candidateTrackIndex];
                if (!candidateTrack.Locked && TrackCompatibility.IsItemCompatibleWithTrack(_dragItem.Kind, candidateTrack.Kind))
                    _dragTrackIndex = candidateTrackIndex;
            }

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
        if (IsMouseCaptured) ReleaseMouseCapture();
        Cursor = Cursors.Arrow;

        if (_isPanning)
        {
            _isPanning = false;
            e.Handled = true;
        }

        if (_isDraggingClip && _dragItem != null)
        {
            var item = _dragItem;
            var newStart = item.TimelineStart;
            item.TimelineStart = _dragStartTime;
            _isDraggingClip = false;
            ClipMoveRequested?.Invoke(this, new ClipMoveRequestedEventArgs(
                item,
                _selectedTrackIndex,
                _dragTrackIndex,
                newStart));
            e.Handled = true;
        }

        if (_isTrimming && _dragItem != null)
        {
            var item = _dragItem;
            var newStart = item.TimelineStart;
            var newDuration = item.Duration;
            var newSourceStart = item.SourceStart;
            item.TimelineStart = _dragStartTime;
            item.Duration = _dragOrigDuration;
            item.SourceStart = _dragOrigSourceStart;
            _isTrimming = false;
            ClipTrimRequested?.Invoke(this, new ClipTrimRequestedEventArgs(
                item,
                _selectedTrackIndex,
                newStart,
                newDuration,
                newSourceStart));
            e.Handled = true;
        }

        if (_dragFromPlayhead)
        {
            _dragFromPlayhead = false;
            e.Handled = true;
        }

        _dragMode = DragMode.None;
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
            DeleteSelectedClipRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private MediaTime SnapMoveStart(MediaTime candidateStart, MediaTime duration, TimelineItem exclude)
    {
        if (!SnapEnabled) return candidateStart;

        var snappedStart = SnapTime(candidateStart, exclude);
        var candidateEnd = candidateStart.Add(duration);
        var snappedEnd = SnapTime(candidateEnd, exclude);
        var startAdjustment = snappedStart.Seconds - candidateStart.Seconds;
        var endAdjustment = snappedEnd.Seconds - candidateEnd.Seconds;
        var adjustment = Math.Abs(startAdjustment) <= Math.Abs(endAdjustment)
            ? startAdjustment
            : endAdjustment;

        return MediaTime.FromSeconds(Math.Max(0, candidateStart.Seconds + adjustment));
    }

    private MediaTime SnapTime(MediaTime candidate, TimelineItem exclude)
    {
        if (!SnapEnabled || Sequence == null) return candidate;

        var thresholdSeconds = 10.0 / Math.Max(1, _viewport.PixelsPerSecond);
        var bestSeconds = candidate.Seconds;
        var bestDistance = thresholdSeconds + double.Epsilon;

        void Consider(MediaTime target)
        {
            var distance = Math.Abs(target.Seconds - candidate.Seconds);
            if (distance <= thresholdSeconds && distance < bestDistance)
            {
                bestDistance = distance;
                bestSeconds = target.Seconds;
            }
        }

        Consider(MediaTime.Zero);
        foreach (var marker in Sequence.Markers)
            Consider(marker.Time);

        foreach (var item in Sequence.Tracks.SelectMany(track => track.Items))
        {
            if (item.Id == exclude.Id) continue;
            Consider(item.TimelineStart);
            Consider(item.TimelineEnd);
        }

        return MediaTime.FromSeconds(bestSeconds);
    }

    private int GetTrackIndexAtY(double y)
    {
        if (Sequence == null || y < _viewport.RulerHeight) return -1;
        var index = _viewport.YToTrackIndex(y);
        return index >= 0 && index < Sequence.Tracks.Count ? index : -1;
    }

    private Marker? HitTestMarker(double x)
    {
        if (Sequence == null) return null;
        const double threshold = 7;
        return Sequence.Markers
            .Select(marker => new { Marker = marker, Distance = Math.Abs(_viewport.TimeToPixel(marker.Time) - x) })
            .Where(candidate => candidate.Distance <= threshold)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Marker)
            .FirstOrDefault();
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

public sealed class ClipMoveRequestedEventArgs : EventArgs
{
    public ClipMoveRequestedEventArgs(TimelineItem item, int sourceTrackIndex, int targetTrackIndex, MediaTime newStart)
    {
        Item = item;
        SourceTrackIndex = sourceTrackIndex;
        TargetTrackIndex = targetTrackIndex;
        NewStart = newStart;
    }

    public TimelineItem Item { get; }
    public int SourceTrackIndex { get; }
    public int TargetTrackIndex { get; }
    public MediaTime NewStart { get; }
}

public sealed class ClipTrimRequestedEventArgs : EventArgs
{
    public ClipTrimRequestedEventArgs(
        TimelineItem item,
        int trackIndex,
        MediaTime newStart,
        MediaTime newDuration,
        MediaTime newSourceStart)
    {
        Item = item;
        TrackIndex = trackIndex;
        NewStart = newStart;
        NewDuration = newDuration;
        NewSourceStart = newSourceStart;
    }

    public TimelineItem Item { get; }
    public int TrackIndex { get; }
    public MediaTime NewStart { get; }
    public MediaTime NewDuration { get; }
    public MediaTime NewSourceStart { get; }
}
