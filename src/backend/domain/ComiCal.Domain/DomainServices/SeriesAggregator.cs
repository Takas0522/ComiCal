namespace ComiCal.Domain.DomainServices;

public sealed record SeriesAggregateKey(string NormalizedTitle, Guid PrimaryAuthorId);

public static class SeriesAggregator
{
    public static string ComputeNormalizedTitle(string rawTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawTitle);
        // Strip volume number suffix before normalizing
        var stripped = VolumeNumberExtractor.Extract(rawTitle) is not null
            ? StripVolumeNumber(rawTitle)
            : rawTitle;
        return TitleNormalizer.Normalize(stripped);
    }

    public static Guid ExtractPrimaryAuthor(IReadOnlyList<Guid> authorIds)
    {
        if (authorIds is null || authorIds.Count == 0)
            throw new ArgumentException("At least one author ID is required.", nameof(authorIds));
        return authorIds[0];
    }

    public static SeriesAggregateKey ComputeKey(string rawTitle, IReadOnlyList<Guid> authorIds)
        => new(ComputeNormalizedTitle(rawTitle), ExtractPrimaryAuthor(authorIds));

    private static string StripVolumeNumber(string title)
    {
        // Remove common volume suffixes
        var patterns = new[]
        {
            @"\s*第\d+巻.*$",
            @"\s*\(\d+\).*$",
            @"\s*（\d+）.*$",
            @"\s+\d+$"
        };
        var result = title;
        foreach (var pattern in patterns)
        {
            var stripped = System.Text.RegularExpressions.Regex.Replace(result, pattern, string.Empty).Trim();
            if (stripped.Length > 0)
            {
                result = stripped;
                break;
            }
        }
        return result;
    }
}
