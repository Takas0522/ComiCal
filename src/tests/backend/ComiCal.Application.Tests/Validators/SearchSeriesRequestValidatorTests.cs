using ComiCal.Application.Validators;
using ComiCal.Domain.Queries;
using Xunit;

namespace ComiCal.Application.Tests.Validators;

public sealed class SearchSeriesRequestValidatorTests
{
    private readonly SearchSeriesRequestValidator _sut = new();

    [Fact]
    public void Validate_ValidQuery_NoErrors()
    {
        // Arrange
        var query = new SeriesSearchQuery(Q: "進撃の巨人", ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: 20);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullQ_NoErrors()
    {
        // Arrange
        var query = new SeriesSearchQuery(Q: null, ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: 20);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_QExceeds100Chars_ValidationError()
    {
        // Arrange
        var query = new SeriesSearchQuery(
            Q: new string('あ', 101),
            ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: 20);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SeriesSearchQuery.Q));
    }

    [Fact]
    public void Validate_QExactly100Chars_NoErrors()
    {
        // Arrange
        var query = new SeriesSearchQuery(
            Q: new string('あ', 100),
            ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: 20);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageSizeBelowMin_ValidationError(int pageSize)
    {
        // Arrange
        var query = new SeriesSearchQuery(Q: null, ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: pageSize);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SeriesSearchQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSize51_ValidationError()
    {
        // Arrange
        var query = new SeriesSearchQuery(Q: null, ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: 51);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SeriesSearchQuery.PageSize));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    public void Validate_ValidPageSize_NoErrors(int pageSize)
    {
        // Arrange
        var query = new SeriesSearchQuery(Q: null, ReleaseFrom: null, Publisher: null, Cursor: null, PageSize: pageSize);

        // Act
        var result = _sut.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
