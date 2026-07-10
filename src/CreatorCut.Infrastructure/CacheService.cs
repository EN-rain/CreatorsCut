using CreatorCut.Domain;

namespace CreatorCut.Infrastructure;

public sealed class CacheService
{
    private readonly string _cacheRoot;
    private readonly CachePolicy _policy;

    public CacheService(string cacheRoot, CachePolicy policy)
    {
        _cacheRoot = cacheRoot;
        _policy = policy;
        Directory.CreateDirectory(_cacheRoot);
    }

    public string ThumbnailPath(MediaAssetId id) => Path.Combine(_cacheRoot, "thumbnails", $"{id}.jpg");
    public string WaveformPath(MediaAssetId id) => Path.Combine(_cacheRoot, "waveforms", $"{id}.dat");
    public string ProxyPath(MediaAssetId id) => Path.Combine(_cacheRoot, "proxy", $"{id}.mp4");
    public string FrameCachePath(MediaAssetId id, MediaTime time) =>
        Path.Combine(_cacheRoot, "frames", $"{id}_{time.Numerator}_{time.Denominator}.rgb");

    public bool TryGetCached<T>(string path, out T? value) where T : class
    {
        if (File.Exists(path))
        {
            value = null;
            return true;
        }
        value = null;
        return false;
    }

    public async Task EvictOldEntries(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - _policy.CacheEvictionAge;
        foreach (var dir in Directory.GetDirectories(_cacheRoot))
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                if (ct.IsCancellationRequested) return;
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                    File.Delete(file);
            }
        }
    }

    public long GetTotalCacheSize()
    {
        long total = 0;
        foreach (var file in Directory.GetFiles(_cacheRoot, "*", SearchOption.AllDirectories))
            total += new FileInfo(file).Length;
        return total;
    }

    public void ClearAll()
    {
        foreach (var dir in Directory.GetDirectories(_cacheRoot))
            Directory.Delete(dir, recursive: true);
        foreach (var file in Directory.GetFiles(_cacheRoot))
            File.Delete(file);
    }
}
