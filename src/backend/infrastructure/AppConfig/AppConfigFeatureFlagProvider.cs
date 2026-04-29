using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FeatureManagement;

namespace ComiCal.Infrastructure.AppConfig;

/// <summary>
/// Resolves feature flags via <see cref="IFeatureManager"/> (which is backed by
/// Azure App Configuration in production via <c>AddAzureAppConfiguration</c> +
/// <c>UseFeatureFlags</c>, or by the local <c>FeatureManagement</c>
/// configuration section during local development) and caches the materialised
/// map in-process for 30 seconds to absorb high-frequency reads from the
/// <c>/api/feature-flags</c> bootstrap endpoint.
/// </summary>
public sealed class AppConfigFeatureFlagProvider : IFeatureFlagProvider
{
    public static readonly ImmutableArray<string> KnownFlags =
    [
        "qr-sync-enabled",
        "affiliate-link-enabled",
        "purchase-history-export",
        "dark-mode-system-aware",
        "calendar-share-link",
    ];

    private const string CacheKey = "ComiCal.FeatureFlags.All";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IFeatureManager _featureManager;
    private readonly IMemoryCache _cache;

    public AppConfigFeatureFlagProvider(IFeatureManager featureManager, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(featureManager);
        ArgumentNullException.ThrowIfNull(cache);
        _featureManager = featureManager;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
        {
            return cached;
        }

        var map = new Dictionary<string, bool>(KnownFlags.Length, StringComparer.Ordinal);
        foreach (var flag in KnownFlags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            map[flag] = await _featureManager.IsEnabledAsync(flag).ConfigureAwait(false);
        }

        IReadOnlyDictionary<string, bool> snapshot = map;
        _cache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
        });
        return snapshot;
    }

    public async Task<bool> IsEnabledAsync(string featureKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureKey);
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.TryGetValue(featureKey, out var enabled) && enabled;
    }
}
