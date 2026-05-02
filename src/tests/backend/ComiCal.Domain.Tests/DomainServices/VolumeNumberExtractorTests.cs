using ComiCal.Domain.DomainServices;
using Xunit;

namespace ComiCal.Domain.Tests.DomainServices;

public sealed class VolumeNumberExtractorTests
{
    [Theory]
    [InlineData("進撃の巨人 第10巻", 10)]
    [InlineData("鬼滅の刃 第1巻", 1)]
    [InlineData("ワンピース 第100巻", 100)]
    public void Extract_KanjiVolumePattern_ReturnsNumber(string title, int expected)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("進撃の巨人(10)", 10)]
    [InlineData("ONE PIECE(100)", 100)]
    public void Extract_HalfWidthParenPattern_ReturnsNumber(string title, int expected)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("進撃の巨人（10）", 10)]
    [InlineData("ワンピース（100）", 100)]
    public void Extract_FullWidthParenPattern_ReturnsNumber(string title, int expected)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("進撃の巨人 10", 10)]
    [InlineData("ワンピース 100", 100)]
    public void Extract_TrailingNumber_ReturnsNumber(string title, int expected)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("進撃の巨人")]
    [InlineData("タイトルなし")]
    [InlineData("NormalTitle")]
    public void Extract_NoVolumeNumber_ReturnsNull(string title)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Extract_NullOrWhiteSpace_ReturnsNull(string? title)
    {
        // Act
        var result = VolumeNumberExtractor.Extract(title!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Extract_KanjiPatternTakesPriorityOverTrailing()
    {
        // Arrange — "第10巻" should match before trailing number
        var title = "進撃の巨人 第10巻 5";

        // Act
        var result = VolumeNumberExtractor.Extract(title);

        // Assert
        Assert.Equal(10, result);
    }
}
