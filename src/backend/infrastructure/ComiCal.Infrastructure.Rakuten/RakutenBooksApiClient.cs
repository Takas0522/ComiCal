using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

public sealed partial class RakutenBooksApiClient
{
    private readonly HttpClient _httpClient;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<RakutenBooksApiClient> _logger;
    private readonly RakutenAuthCredentials _credentials;
    private const string BaseUrl = "https://openapi.rakuten.co.jp/services/api/BooksBook/Search/20170404";

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
        ILogger<RakutenBooksApiClient> logger,
        RakutenAuthCredentials credentials)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _credentials = credentials;
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
            ["applicationId"] = _credentials.ApplicationId,
            ["accessKey"] = _credentials.AccessKey,
            ["affiliateId"] = _credentials.AffiliateId,
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

        var json = await GetJsonAsync(url, ct);
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
            ["applicationId"] = _credentials.ApplicationId,
            ["accessKey"] = _credentials.AccessKey,
            ["affiliateId"] = _credentials.AffiliateId,
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["hits"] = "30",
            ["formatVersion"] = "2",
        };

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogSearchingKeyword(_logger, keyword, null);

        var json = await GetJsonAsync(url, ct);
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
            ["applicationId"] = _credentials.ApplicationId,
            ["accessKey"] = _credentials.AccessKey,
            ["affiliateId"] = _credentials.AffiliateId,
            ["formatVersion"] = "2",
        };

        var qs = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var url = $"{BaseUrl}?{qs}";

        LogSearchingIsbn(_logger, isbn13, null);

        var json = await GetJsonAsync(url, ct);
        return JsonSerializer.Deserialize<RakutenBooksSearchResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Rakuten Books response");
    }

    private string GetApplicationId()
        => _httpClient.DefaultRequestHeaders.TryGetValues("X-Rakuten-AppId", out var vals)
            ? vals.FirstOrDefault() ?? string.Empty
            : string.Empty;

    private async Task<string> GetJsonAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            var detail = ex is OperationCanceledException
                ? "Request timed out or was canceled."
                : "Transport error; exception message redacted.";
            LogRequestExecutionFailure(_logger, ex.GetType().Name, detail, null);
            throw;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var diagnostic = await GetSafeErrorDiagnosticAsync(response, ct);
                LogRequestRejected(
                    _logger,
                    (int)response.StatusCode,
                    response.Headers.RetryAfter?.ToString() ?? "none",
                    diagnostic,
                    null);

                throw new HttpRequestException(
                    $"Rakuten Books API returned {(int)response.StatusCode} ({response.StatusCode}).",
                    null,
                    response.StatusCode);
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
    }

    private static async Task<string> GetSafeErrorDiagnosticAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return BuildFallbackDiagnostic(body, response, "Non-object JSON response.");

            if (TryExtractDiagnostic(document.RootElement, depth: 0, out var extracted))
            {
                return Truncate(extracted!.ReplaceLineEndings(" ").Trim(), 512);
            }

            // Rakuten sometimes returns 403 with an envelope that does not include the
            // documented "error"/"error_description" fields (for example a Header/Body
            // wrapper, or a plain rate-limit page). Fall back to a truncated raw body
            // snapshot so the actual message reaches Application Insights.
            return BuildFallbackDiagnostic(body, response, "JSON response without a recognized diagnostic field.");
        }
        catch (JsonException)
        {
            return BuildFallbackDiagnostic(
                body,
                response,
                $"Non-JSON response ({response.Content.Headers.ContentType?.MediaType ?? "unknown content type"}).");
        }
    }

    // Recursively look for common error-descriptor fields. Rakuten's non-2xx responses
    // occasionally nest the diagnostic inside a wrapper object such as { "Header": {...},
    // "Body": { "error": "wrong_parameter", "error_description": "..." } }.
    private static bool TryExtractDiagnostic(JsonElement element, int depth, out string? value)
    {
        value = null;
        if (depth > 3 || element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var propertyName in new[] { "error_description", "errorMessage", "error", "message", "errorCode", "code" })
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                var text = property.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    value = text;
                    return true;
                }
            }
        }

        foreach (var child in element.EnumerateObject())
        {
            if (child.Value.ValueKind == JsonValueKind.Object &&
                TryExtractDiagnostic(child.Value, depth + 1, out value))
            {
                return true;
            }
        }

        return false;
    }

    // Emit a short prefix of the raw response body so operators can see what the
    // upstream actually returned. The body does not contain client credentials
    // (those are only in the outbound URL and are never included here).
    private static string BuildFallbackDiagnostic(string body, HttpResponseMessage response, string reason)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "unknown";
        var trimmed = string.IsNullOrEmpty(body)
            ? "(empty body)"
            : Truncate(body.ReplaceLineEndings(" ").Trim(), 256);
        return $"{reason} contentType={mediaType} bodyPrefix={trimmed}";
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rakuten Books API rejected a request: status={StatusCode}, retryAfter={RetryAfter}, diagnostic={Diagnostic}")]
    private static partial void LogRequestRejected(
        ILogger logger,
        int statusCode,
        string retryAfter,
        string diagnostic,
        Exception? exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Rakuten Books API request failed before receiving a response: exceptionType={ExceptionType}, detail={Detail}")]
    private static partial void LogRequestExecutionFailure(
        ILogger logger,
        string exceptionType,
        string detail,
        Exception? exception);
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
