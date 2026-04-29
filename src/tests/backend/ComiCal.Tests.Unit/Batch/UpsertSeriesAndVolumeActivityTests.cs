using ComiCal.Batch.Internal;
using FluentAssertions;
using Xunit;

namespace ComiCal.Tests.Unit.Batch;

public sealed class BatchTextNormalizerTests
{
    [Theory]
    [InlineData("尾田 栄一郎", "尾田 栄一郎")]
    [InlineData("尾田 栄一郎／鈴木 一郎", "尾田 栄一郎")]
    [InlineData("A, B, C", "A")]
    [InlineData("田中、佐藤", "田中")]
    [InlineData("  spaced  ", "spaced")]
    public void ResolvePrimaryAuthorName_returns_first_token(string raw, string expected)
    {
        BatchTextNormalizer.ResolvePrimaryAuthorName(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ResolvePrimaryAuthorName_returns_unknown_for_blank(string? raw)
    {
        BatchTextNormalizer.ResolvePrimaryAuthorName(raw).Should().Be("(不明)");
    }

    [Fact]
    public void Normalize_lowercases_and_trims()
    {
        BatchTextNormalizer.Normalize("  ONE Piece  ").Should().Be("one piece");
    }

    [Fact]
    public void Normalize_throws_for_null()
    {
        var act = () => BatchTextNormalizer.Normalize(null!);
        act.Should().Throw<System.ArgumentNullException>();
    }
}
