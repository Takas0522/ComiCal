using System;
using System.Globalization;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ComiCal.Api.Common;

/// <summary>
/// Helpers for weak-ETag generation and HTTP 304 short-circuiting on
/// <c>If-None-Match</c>. SHA-1 is used purely as a fast non-cryptographic hash; it
/// is NOT used for any security purpose here.
/// </summary>
public static class EtagSupport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serialises <paramref name="value"/> as JSON, computes a weak ETag, and
    /// returns either a 304 (when If-None-Match matches) or a 200 with the body
    /// and <c>ETag</c> header populated.
    /// </summary>
    /// <param name="cacheControl">Optional Cache-Control header value applied to
    /// both 200 and 304 responses. Use only for endpoints whose payload is safe
    /// to share across users (anonymous reads); never set on user-scoped data.</param>
    public static async Task<IActionResult> BuildResponseAsync<T>(HttpRequest request, T value, string? cacheControl = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var json = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var etag = ComputeWeakEtag(json);

        if (TryReadIfNoneMatch(request, out var ifNoneMatch) && IfNoneMatchMatches(ifNoneMatch, etag))
        {
            request.HttpContext.Response.Headers[HeaderNames.ETag] = etag;
            if (cacheControl is not null)
            {
                request.HttpContext.Response.Headers[HeaderNames.CacheControl] = cacheControl;
            }
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        request.HttpContext.Response.Headers[HeaderNames.ETag] = etag;
        if (cacheControl is not null)
        {
            request.HttpContext.Response.Headers[HeaderNames.CacheControl] = cacheControl;
        }
        await Task.CompletedTask.ConfigureAwait(false);
        return new ContentResult
        {
            Content = System.Text.Encoding.UTF8.GetString(json),
            ContentType = MediaTypeNames.Application.Json,
            StatusCode = StatusCodes.Status200OK,
        };
    }

    /// <summary>Computes <c>W/"&lt;sha1-hex&gt;"</c> over the given UTF-8 bytes.</summary>
    public static string ComputeWeakEtag(ReadOnlySpan<byte> body)
    {
        Span<byte> hash = stackalloc byte[20];
        if (!SHA1.TryHashData(body, hash, out _))
        {
            // Defensive: SHA1 with a 20-byte destination always succeeds.
            throw new InvalidOperationException("Failed to compute ETag hash.");
        }

        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Create(CultureInfo.InvariantCulture, $"W/\"{hex}\"");
    }

    private static bool TryReadIfNoneMatch(HttpRequest request, out string value)
    {
        if (request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var values) && values.Count > 0)
        {
            value = values.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }

    private static bool IfNoneMatchMatches(string ifNoneMatch, string etag)
    {
        if (ifNoneMatch == "*")
        {
            return true;
        }

        // Comma-separated list of (weak/strong) ETags. We compare ignoring W/ prefix
        // because the client may strip or preserve it.
        var span = ifNoneMatch.AsSpan();
        while (!span.IsEmpty)
        {
            var commaIdx = span.IndexOf(',');
            var token = (commaIdx < 0 ? span : span[..commaIdx]).Trim();
            if (TokensMatch(token, etag))
            {
                return true;
            }

            if (commaIdx < 0)
            {
                break;
            }

            span = span[(commaIdx + 1)..];
        }

        return false;
    }

    private static bool TokensMatch(ReadOnlySpan<char> incoming, string etag)
    {
        var stripped = StripWeakPrefix(incoming);
        var ours = StripWeakPrefix(etag.AsSpan());
        return stripped.SequenceEqual(ours);
    }

    private static ReadOnlySpan<char> StripWeakPrefix(ReadOnlySpan<char> value)
    {
        if (value.Length >= 2 && (value[0] == 'W' || value[0] == 'w') && value[1] == '/')
        {
            value = value[2..];
        }

        return value.Trim();
    }
}
