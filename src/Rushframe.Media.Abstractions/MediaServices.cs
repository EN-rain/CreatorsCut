namespace Rushframe.Media.Abstractions;

public enum MediaStreamKind
{
    Video,
    Audio,
    Subtitle,
    Other,
}

public sealed record MediaStreamInfo(
    MediaStreamKind Kind,
    string Codec,
    int? Width = null,
    int? Height = null,
    double? FrameRate = null,
    int? Channels = null,
    int? SampleRate = null);

public sealed record MediaProbeResult(
    string Path,
    TimeSpan Duration,
    long SizeBytes,
    IReadOnlyList<MediaStreamInfo> Streams)
{
    public bool HasVideo => Streams.Any(s => s.Kind == MediaStreamKind.Video);
    public bool HasAudio => Streams.Any(s => s.Kind == MediaStreamKind.Audio);
}

public sealed record MediaJobProgress(double Percent, string Message);

public sealed record ProxyRequest(string SourcePath, string OutputPath, int MaxHeight);

public sealed record ThumbnailRequest(string SourcePath, string OutputPath, TimeSpan Time);

public sealed record WaveformRequest(string SourcePath, string OutputPath, int Width = 1200, int Height = 180);

public sealed record ExportRequest(
    IReadOnlyList<string> SourcePaths,
    string OutputPath,
    int Width,
    int Height,
    double FrameRate);

public interface IMediaProbeService
{
    Task<MediaProbeResult> ProbeAsync(string path, CancellationToken cancellationToken = default);
}

public interface IMediaDerivativeService
{
    Task GenerateProxyAsync(
        ProxyRequest request,
        IProgress<MediaJobProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task GenerateThumbnailAsync(
        ThumbnailRequest request,
        CancellationToken cancellationToken = default);

    Task GenerateWaveformAsync(
        WaveformRequest request,
        CancellationToken cancellationToken = default);
}

public interface IMediaExportService
{
    Task ExportAsync(
        ExportRequest request,
        IProgress<MediaJobProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
