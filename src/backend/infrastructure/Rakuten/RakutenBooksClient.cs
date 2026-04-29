using System.Globalization;
using System.Net;
using System.Text.Json;
using ComiCal.Infrastructure.Rakuten.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// 楽天 Books API（Books Total Search 20170404）の typed HttpClient 実装。
/// </summary>
public sealed class RakutenBooksClient : IRakutenBooksClient
{
    /// <summary>DI で登録される <see cref="HttpClient"/> 名。</summary>
    public const string HttpClientName = "rakuten-books";

    private const string SearchPath = "services/api/BooksTotal/Search/20170404";
    private const string MangaGenreId = "001001";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly RakutenBooksOptions _options;
    private readonly ILogger<RakutenBooksClient> _logger;

    public RakutenBooksClient(
        HttpClient httpClient,
        IOptions<RakutenBooksOptions> options,
        ILogger<RakutenBooksClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _http = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RakutenSearchResponse> SearchByGenreAsync(
        string keyword,
        int page,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "page must be >= 1");
        }

        var url = BuildUrl(new[]
        {
            ("keyword", keyword),
            ("bookGenreId", MangaGenreId),
            ("page", page.ToString(CultureInfo.InvariantCulture)),
            ("applicationId", _options.ApplicationId),
            ("affiliateId", _options.AffiliateId ?? string.Empty),
            ("format", "json"),
            ("formatVersion", "2"),
        });

        return await SendAsync<RakutenSearchResponse>(url, cancellationToken).ConfigureAwait(false)
            ?? new RakutenSearchResponse();
    }

    public async Task<RakutenItem?> GetByIsbnAsync(string isbn13, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn13);
        var url = BuildUrl(new[]
        {
            ("isbn", isbn13),
            ("applicationId", _options.ApplicationId),
            ("affiliateId", _options.AffiliateId ?? string.Empty),
            ("format", "json"),
            ("formatVersion", "2"),
        });

        var resp = await SendAsync<RakutenSearchResponse>(url, cancellationToken).ConfigureAwait(false);
        return resp?.Items.Count > 0 ? resp.Items[0].Item : null;
    }

    private async Task<T?> SendAsync<T>(string url, CancellationToken cancellationToken)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Rakuten Books API request failed: {Url}", url);
            throw new RakutenBooksApiException(
                "Rakuten Books API call failed after retries.", statusCode: null, innerException: ex);
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            // 5xx and 429 should already be retried by the resilience pipeline; if we land
            // here it means retries are exhausted.
            _logger.LogWarning(
                "Rakuten Books API returned {StatusCode} after retries for {Url}",
                (int)response.StatusCode, url);
            throw new RakutenBooksApiException(
                $"Rakuten Books API returned HTTP {(int)response.StatusCode}.",
                (int)response.StatusCode);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        try
        {
            return await JsonSerializer
                .DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new RakutenBooksApiException(
                "Failed to parse Rakuten Books API response.", statusCode: null, innerException: ex);
        }
    }

    private static string BuildUrl(IEnumerable<(string Key, string Value)> parameters)
    {
        var qs = string.Join('&', parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{SearchPath}?{qs}";
    }
}
