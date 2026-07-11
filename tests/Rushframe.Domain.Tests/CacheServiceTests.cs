using Rushframe.Infrastructure;

namespace Rushframe.Domain.Tests;

public sealed class CacheServiceTests
{
    [Fact]
    public async Task EvictOldEntries_EnforcesProxySizeLimit()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rushframe-cache-{Guid.NewGuid():N}");
        try
        {
            var policy = new CachePolicy
            {
                ProxyCacheSizeBytes = 10,
                ThumbnailCacheSizeBytes = 1_000_000,
                WaveformCacheSizeBytes = 1_000_000,
                FrameCacheSizeBytes = 1_000_000,
                CacheEvictionAge = TimeSpan.FromDays(30),
            };
            var service = new CacheService(root, policy);
            var proxyDir = Path.Combine(root, "proxy");
            Directory.CreateDirectory(proxyDir);
            var oldFile = Path.Combine(proxyDir, "old.mp4");
            var newFile = Path.Combine(proxyDir, "new.mp4");
            await File.WriteAllBytesAsync(oldFile, new byte[9]);
            await File.WriteAllBytesAsync(newFile, new byte[9]);
            File.SetLastAccessTimeUtc(oldFile, DateTime.UtcNow.AddDays(-2));
            File.SetLastAccessTimeUtc(newFile, DateTime.UtcNow);

            await service.EvictOldEntries(CancellationToken.None);

            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(newFile));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
