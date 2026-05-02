using ComiCal.Application.UseCases.Subscriptions;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class AddSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _subRepo = Substitute.For<ISubscriptionRepository>();
    private readonly ISeriesRepository _seriesRepo = Substitute.For<ISeriesRepository>();
    private readonly AddSubscriptionUseCase _sut;

    public AddSubscriptionUseCaseTests()
    {
        _sut = new AddSubscriptionUseCase(_subRepo, _seriesRepo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSeriesNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        _seriesRepo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns((Series?)null);

        // Act
        var result = await _sut.ExecuteAsync(userId, seriesId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadySubscribed_ReturnsConflict()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var series = Series.Create("タイトル", "title", Guid.NewGuid());
        _seriesRepo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(series);

        var existingSub = Subscription.Create(userId, seriesId);
        _subRepo.FindAsync(userId, seriesId, Arg.Any<CancellationToken>()).Returns(existingSub);

        // Act
        var result = await _sut.ExecuteAsync(userId, seriesId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_Success_ReturnsSubscriptionDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var series = Series.Create("タイトル", "title", Guid.NewGuid());
        _seriesRepo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(series);
        _subRepo.FindAsync(userId, seriesId, Arg.Any<CancellationToken>()).Returns((Subscription?)null);
        _subRepo.UpsertAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Subscription>().SubscriptionId);

        // Act
        var result = await _sut.ExecuteAsync(userId, seriesId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(seriesId, result.Value.SeriesId);
    }

    [Fact]
    public async Task ExecuteAsync_SoftDeletedSubscription_AllowsResubscribe()
    {
        // Arrange — a subscription that was soft-deleted should be treated as absent
        var userId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var series = Series.Create("タイトル", "title", Guid.NewGuid());
        _seriesRepo.FindByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(series);

        var deletedSub = Subscription.Create(userId, seriesId);
        deletedSub.SoftDelete();
        _subRepo.FindAsync(userId, seriesId, Arg.Any<CancellationToken>()).Returns(deletedSub);
        _subRepo.UpsertAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Subscription>().SubscriptionId);

        // Act
        var result = await _sut.ExecuteAsync(userId, seriesId);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
