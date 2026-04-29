using System;
using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace ComiCal.Api.Common;

/// <summary>Helpers that pull and parse query-string values for Function triggers.</summary>
internal static class QueryParameters
{
    public static string? GetString(HttpRequest request, string key)
    {
        if (request.Query.TryGetValue(key, out var values) && values.Count > 0)
        {
            var v = values[0];
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        return null;
    }

    public static Guid? GetGuid(HttpRequest request, string key)
    {
        var raw = GetString(request, key);
        if (raw is null)
        {
            return null;
        }

        if (Guid.TryParse(raw, out var g))
        {
            return g;
        }

        throw BadParam(key, "must be a valid GUID.");
    }

    public static int GetInt(HttpRequest request, string key, int defaultValue)
    {
        var raw = GetString(request, key);
        if (raw is null)
        {
            return defaultValue;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
        {
            return i;
        }

        throw BadParam(key, "must be an integer.");
    }

    public static DateOnly? GetDate(HttpRequest request, string key)
    {
        var raw = GetString(request, key);
        if (raw is null)
        {
            return null;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d;
        }

        throw BadParam(key, "must be an ISO-8601 date (yyyy-MM-dd).");
    }

    /// <summary>Parses <c>YYYY-MM</c> into the first day of that month.</summary>
    public static DateOnly? GetMonth(HttpRequest request, string key)
    {
        var raw = GetString(request, key);
        if (raw is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(raw, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
        {
            return new DateOnly(d.Year, d.Month, 1);
        }

        throw BadParam(key, "must be an ISO month (yyyy-MM).");
    }

    private static ValidationException BadParam(string key, string detail)
        => new(new[] { new ValidationFailure(key, $"Query parameter '{key}' {detail}") });
}

