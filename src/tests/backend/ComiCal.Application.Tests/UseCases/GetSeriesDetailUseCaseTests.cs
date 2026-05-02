using ComiCal.Application.UseCases.Series;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class GetSeriesDetailUseCaseTests
{
    private readonly ISeriesRepository _repo = Substitute.For<ISeriesRepository>();
    private readonly GetSeriesDetailUseCase _sut;

    public GetSeriesDetailUseCaseTests()
    {
        _sut = new GetSeriesDetailUseCase(_repo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSeriesExists_ReturnsSuccess()
    {
        // Arrange
        var seriesId = Guid.NewGuid();
        var series = Series.Create("テストシリーズ", "test-series", Guid.NewGuid());
        _repo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>())
            .Returns(series);

        // Act
        var result = await _sut.ExecuteAsync(seriesId, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テストシリーズ", result.Value.Title);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSeriesNotFound_ReturnsFailure()
    {
        // Arrange
        var seriesId = Guid.NewGuid();
        _repo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>())
            .Returns((Series?)null);

        // Act
        var result = await _sut.ExecuteAsync(seriesId, null);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_PassesBlobBaseUrl_ToMapper()
    {
        // Arrange
        var seriesId = Guid.NewGuid();
        var series = Series.Create("タイトル", "title");
        _repo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(series);

        // Act
        var result = await _sut.ExecuteAsync(seriesId, "https://blob.example.com/");

        // Assert
        Assert.True(result.IsSuccess);
    }
}
