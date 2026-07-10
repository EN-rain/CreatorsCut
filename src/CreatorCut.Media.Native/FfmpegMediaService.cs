using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using CreatorCut.Media.Abstractions;

namespace CreatorCut.Media.Native;

public sealed class FfmpegMediaService : IMediaProbeService, IMediaDerivativeService, IMediaExportService
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    public FfmpegMediaService(string? ffmpegPath = null, string? ffprobePath = null)
    {
        _ffmpegPath = ResolveTool(ffmpegPath, "ffmpeg");
        _ffprobePath = ResolveTool(ffprobePath, "ffprobe");
    }

    public async Task<MediaProbeResult> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Media file not found.", path);

        var output = await RunAsync(
            _ffprobePath,
            $"-v error -print_format json -show_format -show_streams {Quote(path)}",
            cancellationToken);

        using var doc = JsonDocument.Parse(output.StdOut);
        var root = doc.RootElement;
        var streams = new List<MediaStreamInfo>();

        if (root.TryGetProperty("streams", out var streamArray))
        {
            foreach (var stream in streamArray.EnumerateArray())
            {
                var kind = ParseStreamKind(GetString(stream, "codec_type"));
                var codec = GetString(stream, "codec_name") ?? "";
                streams.Add(new MediaStreamInfo(
                    kind,
                    codec,
                    GetInt(stream, "width"),
                    GetInt(stream, "height"),
                    ParseFrameRate(GetString(stream, "avg_frame_rate") ?? GetString(stream, "r_frame_rate")),
                    GetInt(stream, "channels"),
                    GetInt(stream, "sample_rate")));
            }
        }

        var format = root.GetProperty("format");
        var duration = TimeSpan.FromSeconds(GetDouble(format, "duration") ?? 0);
        var size = GetLong(format, "size") ?? new FileInfo(path).Length;

        return new MediaProbeResult(path, duration, size, streams);
    }

    public async Task GenerateProxyAsync(
        ProxyRequest request,
        IProgress<MediaJobProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
        progress?.Report(new MediaJobProgress(0, "Generating proxy"));
        await RunAsync(
            _ffmpegPath,
            $"-y -i {Quote(request.SourcePath)} -vf scale=-2:{request.MaxHeight} -c:v libx264 -preset veryfast -crf 24 -c:a aac -b:a 128k {Quote(request.OutputPath)}",
            cancellationToken);
        progress?.Report(new MediaJobProgress(100, "Proxy complete"));
    }

    public async Task GenerateThumbnailAsync(ThumbnailRequest request, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
        await RunAsync(
            _ffmpegPath,
            $"-y -ss {FormatSeconds(request.Time)} -i {Quote(request.SourcePath)} -frames:v 1 -q:v 2 {Quote(request.OutputPath)}",
            cancellationToken);
    }

    public async Task GenerateWaveformAsync(WaveformRequest request, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
        await RunAsync(
            _ffmpegPath,
            $"-y -i {Quote(request.SourcePath)} -filter_complex \"aformat=channel_layouts=mono,showwavespic=s={request.Width}x{request.Height}:colors=#56B6C2\" -frames:v 1 {Quote(request.OutputPath)}",
            cancellationToken);
    }

    public async Task ExportAsync(
        ExportRequest request,
        IProgress<MediaJobProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (request.SourcePaths.Count == 0) throw new ArgumentException("At least one source is required.", nameof(request));

        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputPath)!);
        progress?.Report(new MediaJobProgress(0, "Exporting"));

        if (request.SourcePaths.Count == 1)
        {
            await RunAsync(
                _ffmpegPath,
                $"-y -i {Quote(request.SourcePaths[0])} -vf scale={request.Width}:{request.Height}:force_original_aspect_ratio=decrease,pad={request.Width}:{request.Height}:(ow-iw)/2:(oh-ih)/2 -r {request.FrameRate.ToString(CultureInfo.InvariantCulture)} -c:v libx264 -preset veryfast -crf 20 -c:a aac -b:a 192k {Quote(request.OutputPath)}",
                cancellationToken);
        }
        else
        {
            var listPath = Path.Combine(Path.GetTempPath(), $"creatorcut-concat-{Guid.NewGuid():N}.txt");
            try
            {
                await File.WriteAllLinesAsync(
                    listPath,
                    request.SourcePaths.Select(p => $"file '{p.Replace("'", "'\\''")}'"),
                    cancellationToken);
                await RunAsync(
                    _ffmpegPath,
                    $"-y -f concat -safe 0 -i {Quote(listPath)} -vf scale={request.Width}:{request.Height}:force_original_aspect_ratio=decrease,pad={request.Width}:{request.Height}:(ow-iw)/2:(oh-ih)/2 -r {request.FrameRate.ToString(CultureInfo.InvariantCulture)} -c:v libx264 -preset veryfast -crf 20 -c:a aac -b:a 192k {Quote(request.OutputPath)}",
                    cancellationToken);
            }
            finally
            {
                if (File.Exists(listPath)) File.Delete(listPath);
            }
        }

        progress?.Report(new MediaJobProgress(100, "Export complete"));
    }

    private static string ResolveTool(string? explicitPath, string toolName)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath)) return explicitPath;

        var local = Path.Combine(AppContext.BaseDirectory, ".tools", "bin", OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName);
        if (File.Exists(local)) return local;

        var repoLocal = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".tools", "bin", OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName));
        return File.Exists(repoLocal) ? repoLocal : toolName;
    }

    private static async Task<(string StdOut, string StdErr)> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed with exit code {process.ExitCode}: {stderr}");

        return (stdout, stderr);
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
    private static string FormatSeconds(TimeSpan value) => value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static MediaStreamKind ParseStreamKind(string? value) => value switch
    {
        "video" => MediaStreamKind.Video,
        "audio" => MediaStreamKind.Audio,
        "subtitle" => MediaStreamKind.Subtitle,
        _ => MediaStreamKind.Other,
    };

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static int? GetInt(JsonElement element, string name)
    {
        var text = GetString(element, name);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static long? GetLong(JsonElement element, string name)
    {
        var text = GetString(element, name);
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static double? GetDouble(JsonElement element, string name)
    {
        var text = GetString(element, name);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static double? ParseFrameRate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "0/0") return null;
        var parts = value.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var num) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var den) &&
            den != 0)
            return num / den;

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}
