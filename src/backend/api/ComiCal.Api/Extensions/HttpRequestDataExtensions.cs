using ComiCal.Shared;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace ComiCal.Api.Extensions;

public static class HttpRequestDataExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<HttpResponseData> ToProblemAsync(
        this HttpRequestData req, Error error, string? traceId = null)
    {
        var (status, type) = error.Code switch
        {
            var c when c.EndsWith(".NotFound", StringComparison.Ordinal) =>
                (HttpStatusCode.NotFound, "https://comical.example.jp/errors/not-found"),
            var c when c.EndsWith(".AlreadyExists", StringComparison.Ordinal) =>
                (HttpStatusCode.Conflict, "https://comical.example.jp/errors/conflict"),
            "Unauthorized" =>
                (HttpStatusCode.Unauthorized, "https://comical.example.jp/errors/unauthorized"),
            "Validation" =>
                (HttpStatusCode.BadRequest, "https://comical.example.jp/errors/validation"),
            _ => (HttpStatusCode.InternalServerError, "https://comical.example.jp/errors/internal")
        };

        var res = req.CreateResponse(status);
        // NOTE: WriteAsJsonAsync overload below sets Content-Type internally; adding it manually
        //       causes a duplicate-value FormatException.
        await res.WriteAsJsonAsync(new
        {
            type,
            title = error.Message,
            status = (int)status,
            traceId
        }, "application/problem+json");
        return res;
    }

    public static async Task<T?> ReadJsonAsync<T>(this HttpRequestData req)
    {
        var body = await req.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return default;
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    public static string? GetQueryParam(this HttpRequestData req, string name)
    {
        var query = req.Url.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
