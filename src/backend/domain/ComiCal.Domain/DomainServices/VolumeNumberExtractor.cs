using System.Text.RegularExpressions;

namespace ComiCal.Domain.DomainServices;

public static partial class VolumeNumberExtractor
{
    [GeneratedRegex(@"第(\d+)巻")]
    private static partial Regex KanjiVolumePattern();

    [GeneratedRegex(@"\((\d+)\)")]
    private static partial Regex HalfWidthParenPattern();

    [GeneratedRegex(@"（(\d+)）")]
    private static partial Regex FullWidthParenPattern();

    [GeneratedRegex(@"\s+(\d+)$")]
    private static partial Regex TrailingNumberPattern();

    public static int? Extract(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var m = KanjiVolumePattern().Match(title);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n1)) return n1;

        m = HalfWidthParenPattern().Match(title);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n2)) return n2;

        m = FullWidthParenPattern().Match(title);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n3)) return n3;

        m = TrailingNumberPattern().Match(title);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n4)) return n4;

        return null;
    }
}
