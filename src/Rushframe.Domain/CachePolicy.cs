namespace Rushframe.Domain;

public sealed class CachePolicy
{
    public long ThumbnailCacheSizeBytes { get; set; } = 500_000_000;
    public long WaveformCacheSizeBytes { get; set; } = 200_000_000;
    public long FrameCacheSizeBytes { get; set; } = 1_000_000_000;
    public long ProxyCacheSizeBytes { get; set; } = 10_000_000_000;
    public int MaxThumbnails { get; set; } = 5000;
    public TimeSpan CacheEvictionAge { get; set; } = TimeSpan.FromDays(7);
}
