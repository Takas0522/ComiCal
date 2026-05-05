using ComiCal.Application.Interfaces;
using System.Collections.Concurrent;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// IRakutenBookSearchService の実装。
/// 短期 in-memory キャッシュ（10 分）を持ち、同一クエリへの重複 API 呼び出しを防ぐ。
/// </summary>
public sealed class RakutenBookSearchService(RakutenBooksApiClient client) : IRakutenBookSearchService
{
    private readonly ConcurrentDictionary<string, (DateTime ExpiresAt, IReadOnlyList<RakutenBookSearchItem> Items)> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    /// キャッシュエントリ数がこの上限を超えたら期限切れエントリを除去する。
    private const int CacheEvictionThreshold = 500;

    public async Task<IReadOnlyList<RakutenBookSearchItem>> SearchByKeywordAsync(
        string keyword, CancellationToken ct = default)
    {
        var cacheKey = $"kw:{keyword.Trim().ToLowerInvariant()}";
        if (TryGetCached(cacheKey, out var cached))
            return cached;

        var result = await client.SearchByKeywordAsync(keyword, page: 1, ct);
        var items = result.Items.Select(ToModel).ToList();
        AddToCache(cacheKey, items);
        return items;
    }

    public async Task<RakutenBookSearchItem?> SearchByIsbnAsync(
        string isbn13, CancellationToken ct = default)
    {
        var cacheKey = $"isbn:{isbn13}";
        if (TryGetCached(cacheKey, out var cached))
            return cached.Count > 0 ? cached[0] : null;

        var result = await client.SearchByIsbnAsync(isbn13, ct);
        var items = result.Items.Select(ToModel).ToList();
        AddToCache(cacheKey, items);
        return items.Count > 0 ? items[0] : null;
    }

    private bool TryGetCached(string key, out IReadOnlyList<RakutenBookSearchItem> items)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            items = entry.Items;
            return true;
        }
        items = [];
        return false;
    }

    private void AddToCache(string key, IReadOnlyList<RakutenBookSearchItem> items)
    {
        _cache[key] = (DateTime.UtcNow.Add(CacheTtl), items);

        // キャッシュエントリ数が上限を超えたら期限切れエントリを除去する
        if (_cache.Count > CacheEvictionThreshold)
        {
            var now = DateTime.UtcNow;
            foreach (var k in _cache.Keys.ToList())
            {
                if (_cache.TryGetValue(k, out var e) && e.ExpiresAt <= now)
                    _cache.TryRemove(k, out _);
            }
        }
    }

    private static RakutenBookSearchItem ToModel(RakutenBookItem item)
        => new(
            item.Isbn,
            item.Title,
            string.IsNullOrWhiteSpace(item.Author) ? null : item.Author,
            string.IsNullOrWhiteSpace(item.PublisherName) ? null : item.PublisherName,
            string.IsNullOrWhiteSpace(item.SalesDate) ? null : item.SalesDate,
            string.IsNullOrWhiteSpace(item.LargeImageUrl) ? null : item.LargeImageUrl,
            string.IsNullOrWhiteSpace(item.ItemUrl) ? null : item.ItemUrl);
}

/// <summary>
/// 楽天 ApplicationId が未設定の場合に使用する no-op 実装。常に空リストを返す。
/// </summary>
internal sealed class NullRakutenBookSearchService : IRakutenBookSearchService
{
    public Task<IReadOnlyList<RakutenBookSearchItem>> SearchByKeywordAsync(string keyword, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<RakutenBookSearchItem>>([]);

    public Task<RakutenBookSearchItem?> SearchByIsbnAsync(string isbn13, CancellationToken ct = default)
        => Task.FromResult<RakutenBookSearchItem?>(null);
}
