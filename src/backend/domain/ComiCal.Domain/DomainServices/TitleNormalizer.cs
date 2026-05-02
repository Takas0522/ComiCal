using System.Text;
using System.Text.RegularExpressions;

namespace ComiCal.Domain.DomainServices;

public static partial class TitleNormalizer
{
    [GeneratedRegex(@"[^\p{L}\p{N}\s]")]
    private static partial Regex SymbolPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesPattern();

    public static string Normalize(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var sb = new StringBuilder(title.Length);
        foreach (var ch in title)
        {
            // Convert full-width alphanumeric/space to half-width
            if (ch >= '！' && ch <= '～')
                sb.Append((char)(ch - '！' + '!'));
            // Convert katakana to hiragana
            else if (ch >= 'ァ' && ch <= 'ン')
                sb.Append((char)(ch - 'ァ' + 'ぁ'));
            else
                sb.Append(ch);
        }

        var normalized = sb.ToString().ToLowerInvariant();
        // Remove symbols and punctuation (but keep letters, digits, and spaces)
        normalized = SymbolPattern().Replace(normalized, " ");
        // Collapse multiple spaces
        normalized = MultipleSpacesPattern().Replace(normalized, " ").Trim();

        return normalized;
    }
}
