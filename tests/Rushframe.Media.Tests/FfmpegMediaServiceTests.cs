using System.Diagnostics;
using Rushframe.Domain;
using Rushframe.Media.Native;

namespace Rushframe.Media.Tests;

public sealed class FfmpegMediaServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"rushframe-media-tests-{Guid.NewGuid():N}");
    private readonly string _ffmpegPath;
    private readonly FfmpegMediaService _service;

    public FfmpegMediaServiceTests()
    {
        Directory.CreateDirectory(_root);
        _ffmpegPath = ResolveFfmpeg();
        _service = new FfmpegMediaService(_ffmpegPath);
    }

    [Fact]
    [Trait("Category", "Media")]
    public async Task ProbeAsync_GeneratedVideo_ReturnsAudioAndVideoStreams()
    {
        var source = await CreateVideoWithAudioAsync();

        var probe = await _service.ProbeAsync(source);

        Assert.True(probe.Duration.TotalSeconds >= 1.8);
        Assert.True(probe.HasVideo);
        Assert.True(probe.HasAudio);
    }

    [Fact]
    [Trait("Category", "Media")]
    public async Task Derivatives_GeneratedVideo_CreateFiles()
    {
        var source = await CreateVideoWithAudioAsync();
        var thumb = Path.Combine(_root, "thumb.jpg");
        var proxy = Path.Combine(_root, "proxy.mp4");
        var waveform = Path.Combine(_root, "waveform.png");

        await _service.GenerateThumbnailAsync(new(source, thumb, TimeSpan.FromSeconds(0.5)));
        await _service.GenerateProxyAsync(new(source, proxy, 120));
        await _service.GenerateWaveformAsync(new(source, waveform, 320, 80));

        Assert.True(new FileInfo(thumb).Length > 0);
        Assert.True(new FileInfo(proxy).Length > 0);
        Assert.True(new FileInfo(waveform).Length > 0);
    }

    [Fact]
    [Trait("Category", "Media")]
    public async Task ExportTimelineAsync_WithAudioTrack_ContainsAudioStream()
    {
        var video = await CreateVideoWithAudioAsync();
        var audio = await CreateToneAsync();
        var output = Path.Combine(_root, "timeline.mp4");

        var project = new Project();
        var videoAsset = new MediaAsset { Kind = MediaKind.Video, OriginalPath = video, RelativeProjectPath = video, Duration = MediaTime.FromSeconds(2) };
        var audioAsset = new MediaAsset { Kind = MediaKind.Audio, OriginalPath = audio, RelativeProjectPath = audio, Duration = MediaTime.FromSeconds(2) };
        project.MediaLibrary.Add(videoAsset);
        project.MediaLibrary.Add(audioAsset);
        var seq = project.MainSequence!;
        seq.Tracks.Add(new Track
        {
            Kind = TrackKind.Video,
            Name = "V1",
            Items =
            {
                new TimelineItem
                {
                    Kind = ItemKind.Clip,
                    MediaAssetId = videoAsset.Id,
                    Duration = MediaTime.FromSeconds(2),
                    SourceDuration = MediaTime.FromSeconds(2),
                },
            },
        });
        seq.Tracks.Add(new Track
        {
            Kind = TrackKind.Audio,
            Name = "A1",
            Items =
            {
                new TimelineItem
                {
                    Kind = ItemKind.Clip,
                    MediaAssetId = audioAsset.Id,
                    Duration = MediaTime.FromSeconds(2),
                    SourceDuration = MediaTime.FromSeconds(2),
                    Volume = 0.8,
                },
            },
        });

        await _service.ExportTimelineAsync(project, seq, output);
        var probe = await _service.ProbeAsync(output);

        Assert.True(File.Exists(output));
        Assert.True(probe.HasVideo);
        Assert.True(probe.HasAudio);
    }

    [Fact]
    [Trait("Category", "Media")]
    public async Task ExtractAudioAsync_GeneratedVideo_CreatesWav()
    {
        var source = await CreateVideoWithAudioAsync();
        var output = Path.Combine(_root, "extract.wav");

        await _service.ExtractAudioAsync(source, output);
        var probe = await _service.ProbeAsync(output);

        Assert.True(new FileInfo(output).Length > 0);
        Assert.True(probe.HasAudio);
    }

    [Fact]
    [Trait("Category", "Media")]
    public async Task GenerateProxyAsync_Cancelled_ThrowsOperationCanceled()
    {
        var source = await CreateVideoWithAudioAsync();
        var output = Path.Combine(_root, "cancelled-proxy.mp4");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.GenerateProxyAsync(new(source, output, 120), cancellationToken: cts.Token));
    }

    private async Task<string> CreateVideoWithAudioAsync()
    {
        var output = Path.Combine(_root, $"video-{Guid.NewGuid():N}.mp4");
        await RunFfmpegAsync("-y -f lavfi -i testsrc=size=160x120:rate=30:duration=2 -f lavfi -i sine=frequency=440:duration=2 -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest", output);
        return output;
    }

    private async Task<string> CreateToneAsync()
    {
        var output = Path.Combine(_root, $"tone-{Guid.NewGuid():N}.wav");
        await RunFfmpegAsync("-y -f lavfi -i sine=frequency=880:duration=2 -acodec pcm_s16le -ar 48000 -ac 2", output);
        return output;
    }

    private async Task RunFfmpegAsync(string args, string output)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"{args} \"{output}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start FFmpeg.");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("FFmpeg fixture generation timed out.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
    }

    private static string ResolveFfmpeg()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".tools", "bin", "ffmpeg.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".tools", "bin", "ffmpeg.exe")),
        };
        var found = candidates.FirstOrDefault(File.Exists);
        if (found != null) return found;
        return "ffmpeg";
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Best effort cleanup for Windows file handles.
        }
    }
}
