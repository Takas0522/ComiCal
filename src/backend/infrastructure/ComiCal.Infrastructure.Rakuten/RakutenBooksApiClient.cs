using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

public sealed class RakutenBooksApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<RakutenBooksApiClient> _logger;
    private const string BaseUrl = "https://app.rakuten.co.jp/services/api/BooksBook/Search/20170404";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Action<ILogger, int, Exception?> LogFetchingPage =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "FetchingPage"), "Fetching Rakuten Books page {Page}");

    private static readonly Action<ILogger, string, Exception?> LogSearchingKeyword =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "SearchingKeyword"), "Searching Rakuten Books by keyword: {Keyword}");

    private static readonly Action<ILogger, string, Exception?> LogSearchingIsbn =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(3, "SearchingIsbn"), "Searching Rakuten Books by ISBN: {Isbn}");

    public RakutenBooksApiClient(
        HttpClient httpClient,
        RateLimiter rateLimiter,
        ILogger<RakutenBooksApiClient> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<RakutenBooksSearchResult> SearchComicsAsync(
        int page, DateOnly? releaseDateFrom, DateOnly? releaseDateTo,
        CancellationToken ct = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken: ct);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit exceeded");

        var queryParams = new Dictionary<string, string>
        {
            ["booksGenreId"] = "001001",
            ["applicationId"] = GetApplicationId(),
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["hits"] = "30",
            ["sort"] = "-releaseDate",
            ["formatVersion"] = "2",
        };

        // NOTE: Rakuten Books Search API does not support releaseDate range filtering.
        // We pass the page only; date filtering is performed client-side from the
        // SalesDate field on each item.
        _ = releaseDateFrom;
        _ = releaseDateTo;

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogFetchingPage(_logger, page, null);

        var json = await _httpClient.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<RakutenBooksSearchResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Rakuten Books response");
    }

    /// <summary>
    /// ジャンル無指定でキーワード検索します。楽天 Books のすべてのジャンルを対象とします。
    /// </summary>
    public async Task<RakutenBooksSearchResult> SearchByKeywordAsync(
        string keyword, int page = 1, CancellationToken ct = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken: ct);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit exceeded");

        var queryParams = new Dictionary<string, string>
        {
            ["title"] = keyword,
            ["applicationId"] = GetApplicationId(),
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["hits"] = "30",
            ["formatVersion"] = "2",
        };

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogSearchingKeyword(_logger, keyword, null);

        var json = await _httpClient.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<RakutenBooksSearchResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Rakuten Books response");
    }

    /// <summary>
    /// ISBN-13 で 1 冊を検索します。ジャンル無指定。
    /// </summary>
    public async Task<RakutenBooksSearchResult> SearchByIsbnAsync(
        string isbn13, CancellationToken ct = default)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken: ct);
        if (!lease.IsAcquired)
            throw new InvalidOperationException("Rate limit exceeded");

        var queryParams = new Dictionary<string, string>
        {
            ["isbn"] = isbn13,
            ["applicationId"] = GetApplicationId(),
            ["formatVersion"] = "2",
        };

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogSearchingIsbn(_logger, isbn13, null);

        var json = await _httpClient.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<RakutenBooksSearchResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Rakuten Books response");
    }

    private string GetApplicationId()
        => _httpClient.DefaultRequestHeaders.TryGetValues("X-Rakuten-AppId", out var vals)
            ? vals.FirstOrDefault() ?? string.Empty
            : string.Empty;
}

public record RakutenBooksSearchResult(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("last")] int Last,
    [property: JsonPropertyName("hits")] int Hits,
    [property: JsonPropertyName("Items")] IReadOnlyList<RakutenBookItem> Items);

public record RakutenBookItem(
    [property: JsonPropertyName("isbn")] string Isbn,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("publisherName")] string PublisherName,
    [property: JsonPropertyName("salesDate")] string SalesDate,
    [property: JsonPropertyName("largeImageUrl")] string LargeImageUrl,
    [property: JsonPropertyName("itemUrl")] string ItemUrl);
