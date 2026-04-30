using ComiCal.Domain.Entities;
using Xunit;

namespace ComiCal.Domain.Tests.Entities;

public sealed class SeriesTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsSeriesWithNonEmptyId()
    {
        // Act
        var series = Series.Create("テストシリーズ", "test-series", Guid.NewGuid());

        // Assert
        Assert.NotEqual(Guid.Empty, series.SeriesId);
        Assert.Equal("テストシリーズ", series.Title);
        Assert.Equal("test-series", series.NormalizedTitle);
    }

    [Fact]
    public void Create_WithoutPublisher_PublisherIdIsNull()
    {
        // Act
        var series = Series.Create("タイトル", "title");

        // Assert
        Assert.Null(series.PublisherId);
    }

    [Theory]
    [InlineData("", "norm")]
    [InlineData("   ", "norm")]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    public void Create_BlankArguments_ThrowsArgumentException(string title, string normalizedTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Series.Create(title, normalizedTitle));
    }

    [Fact]
    public void MarkCompleted_SetsIsCompletedTrue()
    {
        // Arrange
        var series = Series.Create("タイトル", "title");

        // Act
        series.MarkCompleted();

        // Assert
        Assert.True(series.IsCompleted);
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrueAndDeletedAtNotNull()
    {
        // Arrange
        var series = Series.Create("タイトル", "title");

        // Act
        series.SoftDelete();

        // Assert
        Assert.True(series.IsDeleted);
        Assert.NotNull(series.DeletedAt);
    }

    [Fact]
    public void Create_TitleLongerThan512_TruncatedTo512()
    {
        // Arrange
        var longTitle = new string('あ', 600);

        // Act
        var series = Series.Create(longTitle, "norm");

        // Assert
        Assert.Equal(512, series.Title.Length);
    }

    [Fact]
    public void SetPrimaryAuthor_UpdatesAuthorId()
    {
        // Arrange
        var series = Series.Create("タイトル", "title");
        var authorId = Guid.NewGuid();

        // Act
        series.SetPrimaryAuthor(authorId);

        // Assert
        Assert.Equal(authorId, series.PrimaryAuthorId);
    }

    [Fact]
    public void Create_IsCompletedAndIsDeletedDefaultFalse()
    {
        // Act
        var series = Series.Create("タイトル", "title");

        // Assert
        Assert.False(series.IsCompleted);
        Assert.False(series.IsDeleted);
    }
}
