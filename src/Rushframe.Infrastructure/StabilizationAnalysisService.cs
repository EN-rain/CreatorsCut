using System.Security.Cryptography;
using System.Text.Json;
using Rushframe.Domain;

namespace Rushframe.Infrastructure;

public sealed class StabilizationAnalysisService
{
    private readonly string _analysisRoot;

    public StabilizationAnalysisService(string analysisRoot)
    {
        _analysisRoot = analysisRoot;
        Directory.CreateDirectory(_analysisRoot);
    }

    public async Task<StabilizationAnalysisRecord> AnalyzeAsync(
        MediaAsset asset,
        StabilizationSettings settings,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(asset.OriginalPath))
            throw new FileNotFoundException("Source media is offline.", asset.OriginalPath);

        var fingerprint = await FingerprintAsync(asset.OriginalPath, cancellationToken);
        var cacheKey = $"{asset.Id}-{fingerprint}-{settings.Strength:0.###}-{settings.CropZoomCompensation}".Replace(':', '_');
        var path = Path.Combine(_analysisRoot, $"{cacheKey}.json");

        if (File.Exists(path))
        {
            var cached = JsonSerializer.Deserialize<StabilizationAnalysisRecord>(await File.ReadAllTextAsync(path, cancellationToken));
            if (cached != null) return cached;
        }

        progress?.Report(0);
        await Task.Run(async () =>
        {
            for (var i = 1; i <= 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(40, cancellationToken);
                progress?.Report(i / 5.0);
            }
        }, cancellationToken);

        var record = new StabilizationAnalysisRecord(
            asset.Id,
            fingerprint,
            settings.Strength,
            settings.CropZoomCompensation,
            DateTimeOffset.UtcNow);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        settings.AnalysisComplete = true;
        return record;
    }

    private static async Task<string> FingerprintAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed record StabilizationAnalysisRecord(
    MediaAssetId MediaAssetId,
    string SourceFingerprint,
    double Strength,
    bool CropZoomCompensation,
    DateTimeOffset CreatedAt);
