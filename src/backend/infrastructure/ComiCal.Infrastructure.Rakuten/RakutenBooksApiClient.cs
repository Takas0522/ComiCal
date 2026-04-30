using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

public sealed class RakutenBooksApiClient : IDisposable
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
            ["applicationId"] = _httpClient.DefaultRequestHeaders.TryGetValues("X-Rakuten-AppId", out var vals)
                ? vals.FirstOrDefault() ?? string.Empty
                : string.Empty,
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["hits"] = "30",
            ["sort"] = "releaseDate",
        };

        if (releaseDateFrom.HasValue)
            queryParams["releaseDateFrom"] = releaseDateFrom.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (releaseDateTo.HasValue)
            queryParams["releaseDateTo"] = releaseDateTo.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogFetchingPage(_logger, page, null);

        var json = await _httpClient.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize<RakutenBooksSearchResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Rakuten Books response");
    }

    public void Dispose() => _rateLimiter.Dispose();
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
