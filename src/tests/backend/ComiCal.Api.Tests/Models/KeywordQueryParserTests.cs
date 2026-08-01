using ComiCal.Api.Models;
using Xunit;

namespace ComiCal.Api.Tests.Models;

public sealed class KeywordQueryParserTests
{
    [Fact]
    public void Parse_WhenQueryIsMissing_ReturnsEmptyKeywords()
    {
        var result = KeywordQueryParser.Parse(null);

        Assert.True(result.IsValid);
        Assert.Empty(result.Keywords);
    }

    [Fact]
    public void Parse_WhenArrayContainsWhitespace_TrimsAndFiltersEmptyValues()
    {
        var result = KeywordQueryParser.Parse("""["  作品名  "," ","著者名"]""");

        Assert.True(result.IsValid);
        Assert.Equal(["作品名", "著者名"], result.Keywords);
    }

    [Fact]
    public void Parse_WhenArrayContainsEquivalentTerms_DeduplicatesNormalizedTrimmedTerms()
    {
        var result = KeywordQueryParser.Parse("""["  ＡＢＣ  ","ABC","著者名"," 著者名 "]""");

        Assert.True(result.IsValid);
        Assert.Equal(["ABC", "著者名"], result.Keywords);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"keyword\":\"作品名\"}")]
    [InlineData("[\"作品名\", 1]")]
    [InlineData("not-json")]
    public void Parse_WhenQueryIsNotAJsonStringArray_ReturnsInvalid(string query)
    {
        var result = KeywordQueryParser.Parse(query);

        Assert.False(result.IsValid);
        Assert.Empty(result.Keywords);
    }

    [Fact]
    public void Parse_WhenAggregateKeywordLengthExceeds512_ReturnsInvalid()
    {
        var result = KeywordQueryParser.Parse($$"""["{{new string('あ', 513)}}"]""");

        Assert.False(result.IsValid);
        Assert.Empty(result.Keywords);
    }

    [Fact]
    public void Parse_WhenMoreThan16DistinctKeywords_ReturnsInvalid()
    {
        var keywords = string.Join(',', Enumerable.Range(1, 17).Select(number => $"\"keyword{number}\""));

        var result = KeywordQueryParser.Parse($"[{keywords}]");

        Assert.False(result.IsValid);
        Assert.Empty(result.Keywords);
        Assert.Equal("At most 16 distinct keywords are allowed.", result.ErrorMessage);
    }

    [Fact]
    public void Parse_WhenDuplicatesReduceKeywordsToLimit_ReturnsValid()
    {
        var keywords = string.Join(',', Enumerable.Range(1, 16)
            .Select(number => $"\"keyword{number}\"")
            .Append("\" KEYWORD1 \""));

        var result = KeywordQueryParser.Parse($"[{keywords}]");

        Assert.True(result.IsValid);
        Assert.Equal(16, result.Keywords.Count);
    }
}
