using System.Text.Json;
using System.Text;

namespace ComiCal.Api.Models;

public sealed record KeywordQueryParseResult(
    bool IsValid,
    IReadOnlyList<string> Keywords,
    string? ErrorMessage = null);

public static class KeywordQueryParser
{
    private const int MaxAggregateLength = 512;
    private const int MaxKeywordCount = 16;

    public static KeywordQueryParseResult Parse(string? rawQuery)
    {
        if (rawQuery is null)
            return new KeywordQueryParseResult(true, []);

        try
        {
            using var document = JsonDocument.Parse(rawQuery);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return Invalid();

            var keywords = new List<string>();
            var distinctKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String)
                    return Invalid();

                var keyword = element.GetString()!.Normalize(NormalizationForm.FormKC).Trim();
                if (!string.IsNullOrEmpty(keyword) && distinctKeywords.Add(keyword))
                    keywords.Add(keyword);
            }

            if (keywords.Count > MaxKeywordCount)
            {
                return new KeywordQueryParseResult(
                    false,
                    [],
                    $"At most {MaxKeywordCount} distinct keywords are allowed.");
            }

            if (keywords.Sum(keyword => keyword.Length) > MaxAggregateLength)
            {
                return new KeywordQueryParseResult(
                    false,
                    [],
                    $"The combined keyword length must not exceed {MaxAggregateLength} characters.");
            }

            return new KeywordQueryParseResult(true, keywords);
        }
        catch (JsonException)
        {
            return Invalid();
        }
    }

    private static KeywordQueryParseResult Invalid()
        => new(false, [], "q must be a JSON string array.");
}
