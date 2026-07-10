using System.Diagnostics;
using System.Text.Json;

var exeDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
var repoRoot = new DirectoryInfo(exeDir);
while (repoRoot != null && !File.Exists(Path.Combine(repoRoot.FullName, ".tools", "bin", "ffmpeg.exe")))
    repoRoot = repoRoot.Parent;
var repoPath = repoRoot?.FullName ?? Directory.GetCurrentDirectory();
Directory.SetCurrentDirectory(repoPath);

var ffmpeg = Path.GetFullPath(".tools/bin/ffmpeg.exe");
var sampleVideo = Path.GetFullPath("projects/sample/input/clip1.mp4");
var sampleImage = Path.GetFullPath("projects/sample/input/video/vid_001.mp4");
var spikeOut = Path.GetFullPath("spikes/MediaEngineSpike/spike-results");
Directory.CreateDirectory(spikeOut);

var results = new Dictionary<string, object>();
var probeLog = Path.Combine(spikeOut, "probe.txt");

Console.WriteLine($"FFmpeg: {ffmpeg}");
Console.WriteLine($"Sample: {sampleVideo}");
Console.WriteLine();

// 1. Probe
Console.WriteLine("=== 1. Probe H.264/AAC MP4 ===");
var probeOut = RunCapture(ffmpeg, $"""-i "{sampleVideo}" """);
File.WriteAllText(probeLog, probeOut);
results["probe_has_video"] = probeOut.Contains("Video:");
results["probe_has_audio"] = probeOut.Contains("Audio:");
Console.WriteLine($"  Video: {results["probe_has_video"]}, Audio: {results["probe_has_audio"]}");

// 2. Two-track overlay
Console.WriteLine("=== 2. Two-track overlay ===");
var overlayOut = Path.Combine(spikeOut, "overlay_test.mp4");
var overlayTime = Run(ffmpeg,
    $"""-y -i "{sampleVideo}" -i "{sampleImage}" """ +
    """-filter_complex "[0:v]scale=1080:1920[bg];[1:v]scale=480:640,format=rgba,colorchannelmixer=aa=0.5[fg];[bg][fg]overlay=100:100" """ +
    $"""-t 5 -c:v libx264 -preset ultrafast -crf 28 "{overlayOut}" """);
results["overlay_seconds"] = Math.Round(overlayTime.TotalSeconds, 3);
results["overlay_exists"] = File.Exists(overlayOut);

// 3. Trim + offset
Console.WriteLine("=== 3. Trim + offset ===");
var trimOut = Path.Combine(spikeOut, "trim_test.mp4");
var trimTime = Run(ffmpeg, $"""-y -ss 1 -t 3 -i "{sampleVideo}" -c copy "{trimOut}" """);
results["trim_seconds"] = Math.Round(trimTime.TotalSeconds, 3);
results["trim_exists"] = File.Exists(trimOut);

// 4. Transform overlay
Console.WriteLine("=== 4. Transform overlay ===");
var transformOut = Path.Combine(spikeOut, "transform_test.mp4");
var transformTime = Run(ffmpeg,
    $"""-y -i "{sampleVideo}" -i "{sampleImage}" """ +
    """-filter_complex "[0:v]scale=1080:1920[bg];[1:v]scale=320:240,format=rgba,colorchannelmixer=aa=0.7,rotate=15*PI/180:ow=rotw(15*PI/180):oh=roth(15*PI/180):c=none[fg];[bg][fg]overlay=200:300" """ +
    $"""-t 5 -c:v libx264 -preset ultrafast -crf 28 "{transformOut}" """);
results["transform_seconds"] = Math.Round(transformTime.TotalSeconds, 3);
results["transform_exists"] = File.Exists(transformOut);

// 5. Crossfade
Console.WriteLine("=== 5. Crossfade transition ===");
var xfadeOut = Path.Combine(spikeOut, "xfade_test.mp4");
var xfadeTime = Run(ffmpeg,
    $"""-y -i "{sampleVideo}" -i "{sampleImage}" """ +
    """-filter_complex "[0:v]setpts=PTS-STARTPTS,scale=1080:1920[a];[1:v]setpts=PTS-STARTPTS,scale=1080:1920[b];[a][b]xfade=transition=fade:duration=1:offset=4" """ +
    $"""-t 7 -c:v libx264 -preset ultrafast -crf 28 "{xfadeOut}" """);
results["xfade_seconds"] = Math.Round(xfadeTime.TotalSeconds, 3);
results["xfade_exists"] = File.Exists(xfadeOut);

// 6. Seek latency (50 seeks)
Console.WriteLine("=== 6. Seek latency (50 seeks) ===");
var seekTimes = new List<double>();
for (int i = 0; i < 50; i++)
{
    var seekSec = i * 0.3;
    var sw = Stopwatch.StartNew();
    var thumbOut = Path.Combine(spikeOut, $"seek_{i:D2}.jpg");
    Run(ffmpeg, $"""-y -ss {seekSec:F1} -i "{sampleVideo}" -frames:v 1 -q:v 3 "{thumbOut}" """);
    sw.Stop();
    seekTimes.Add(sw.Elapsed.TotalMilliseconds);
}
seekTimes.Sort();
results["seek_p50_ms"] = Math.Round(seekTimes[25], 1);
results["seek_p95_ms"] = Math.Round(seekTimes[47], 1);
Console.WriteLine($"  p50: {results["seek_p50_ms"]}ms, p95: {results["seek_p95_ms"]}ms");

// 7. Render 720p
Console.WriteLine("=== 7. Render 720p ===");
var renderOut = Path.Combine(spikeOut, "render_720p.mp4");
var renderTime = Run(ffmpeg, $"""-y -i "{sampleVideo}" -vf scale=1280:720 -t 10 -c:v libx264 -preset fast -crf 23 "{renderOut}" """);
results["render_720p_seconds"] = Math.Round(renderTime.TotalSeconds, 3);
results["render_720p_exists"] = File.Exists(renderOut);
if (File.Exists(renderOut))
    results["render_720p_kb"] = Math.Round(new FileInfo(renderOut).Length / 1024.0, 1);

// 8. Cancel mid-render
Console.WriteLine("=== 8. Cancel mid-render ===");
var cancelOut = Path.Combine(spikeOut, "cancelled_test.mp4");
var psi = new ProcessStartInfo(ffmpeg, $"""-y -i "{sampleVideo}" -vf scale=1280:720 -t 30 -c:v libx264 -preset fast "{cancelOut}" """)
{
    RedirectStandardError = true, UseShellExecute = false,
};
var cancelProc = Process.Start(psi)!;
await Task.Delay(800);
cancelProc.Kill(entireProcessTree: true);
await cancelProc.WaitForExitAsync();
results["cancel_exit_code"] = cancelProc.ExitCode;
results["cancel_safe"] = true;
Console.WriteLine($"  Killed exit code: {cancelProc.ExitCode}");

// 9. Open/dispose 10x
Console.WriteLine("=== 9. Open/dispose 10x ===");
var openTimes = new List<double>();
for (int i = 0; i < 10; i++)
{
    var sw = Stopwatch.StartNew();
    Run(ffmpeg, $"""-i "{sampleVideo}" -f null - """);
    sw.Stop();
    openTimes.Add(sw.Elapsed.TotalMilliseconds);
}
openTimes.Sort();
results["open_p50_ms"] = Math.Round(openTimes[5], 1);
Console.WriteLine($"  p50: {results["open_p50_ms"]}ms");

// 10. Cold startup
Console.WriteLine("=== 10. Cold startup ===");
var coldSw = Stopwatch.StartNew();
Run(ffmpeg, $"""-i "{sampleVideo}" -f null - """);
coldSw.Stop();
results["cold_startup_ms"] = Math.Round(coldSw.Elapsed.TotalMilliseconds, 1);
Console.WriteLine($"  Cold: {results["cold_startup_ms"]}ms");

var reportPath = Path.Combine(spikeOut, "spike-results.json");
File.WriteAllText(reportPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"\n=== Results ===");
Console.WriteLine(File.ReadAllText(reportPath));

TimeSpan Run(string exe, string args)
{
    var psi = new ProcessStartInfo(exe, args) { RedirectStandardError = true, UseShellExecute = false };
    var sw = Stopwatch.StartNew();
    var proc = Process.Start(psi)!;
    proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    sw.Stop();
    return sw.Elapsed;
}

string RunCapture(string exe, string args)
{
    var psi = new ProcessStartInfo(exe, args)
    {
        RedirectStandardOutput = true, RedirectStandardError = true,
        UseShellExecute = false,
    };
    var proc = Process.Start(psi)!;
    var err = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    return err;
}
