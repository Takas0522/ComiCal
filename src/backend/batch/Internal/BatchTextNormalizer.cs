namespace ComiCal.Batch.Internal;

/// <summary>
/// Pure text-normalization helpers shared by upsert activities. Public so they are
/// unit-testable from the test assembly without exposing internals.
/// </summary>
public static class BatchTextNormalizer
{
    /// <summary>Splits a Rakuten author field on full-width / half-width separators and returns the first non-empty entry.</summary>
    public static string ResolvePrimaryAuthorName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "(不明)";
        }

        var parts = raw.Split(
            ['／', ',', '、'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts[0] : raw.Trim();
    }

    /// <summary>Trim + invariant lower-case for unique-key columns (NormalizedTitle / NormalizedName).</summary>
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return raw.Trim().ToLowerInvariant();
    }
}
