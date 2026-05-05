using ComiCal.Application.Interfaces;
using ComiCal.Application.Services;
using ComiCal.Application.UseCases.Subscriptions;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class AddSubscriptionFromRakutenUseCaseTests
{
    private readonly IRakutenBookSearchService _rakutenSearch = Substitute.For<IRakutenBookSearchService>();
    private readonly SeriesUpsertService _seriesUpsert;
    private readonly ISeriesRepository _seriesRepo = Substitute.For<ISeriesRepository>();
    private readonly ISubscriptionRepository _subRepo = Substitute.For<ISubscriptionRepository>();
    private readonly IAuthorRepository _authorRepo = Substitute.For<IAuthorRepository>();
    private readonly IPublisherRepository _publisherRepo = Substitute.For<IPublisherRepository>();
    private readonly IVolumeRepository _volumeRepo = Substitute.For<IVolumeRepository>();
    private readonly AddSubscriptionFromRakutenUseCase _sut;

    public AddSubscriptionFromRakutenUseCaseTests()
    {
        _seriesUpsert = new SeriesUpsertService(_seriesRepo, _authorRepo, _publisherRepo, _volumeRepo);
        _sut = new AddSubscriptionFromRakutenUseCase(_rakutenSearch, _seriesUpsert, _seriesRepo, _subRepo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsbnInvalid_ReturnsValidationFailure()
    {
        // Arrange / Act
        var result = await _sut.ExecuteAsync(Guid.NewGuid(), "invalid-isbn");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Validation", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]          // 短すぎる
    [InlineData("97840000000012")] // 14 桁
    [InlineData("978400000000X")]  // 数字以外
    public async Task ExecuteAsync_WhenIsbnInvalidFormat_ReturnsValidationFailure(string isbn)
    {
        var result = await _sut.ExecuteAsync(Guid.NewGuid(), isbn);

        Assert.True(result.IsFailure);
        Assert.Contains("Validation", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRakutenBookNotFound_ReturnsNotFound()
    {
        // Arrange
        _rakutenSearch.SearchByIsbnAsync("9784000000001", Arg.Any<CancellationToken>())
            .Returns((RakutenBookSearchItem?)null);

        // Act
        var result = await _sut.ExecuteAsync(Guid.NewGuid(), "9784000000001");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_Success_CreatesSeriesAndSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var isbn = "9784000000001";
        var item = new RakutenBookSearchItem(isbn, "テスト漫画 1", "著者A", "出版社A", null, null, null);

        _rakutenSearch.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(item);

        // seriesRepo: first call (upsert check) returns null; second call returns the series
        var createdSeries = Series.Create("テスト漫画", "testmanga", null);
        _seriesRepo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Series?)null, createdSeries);
        _seriesRepo.UpsertAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Series>().SeriesId);

        // volumeRepo: no existing volume → create
        _volumeRepo.FindByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns((Volume?)null);
        _volumeRepo.UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Volume>().VolumeId);

        // authorRepo: no existing author → create
        _authorRepo.FindByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Author?)null);
        _authorRepo.UpsertAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Author>().AuthorId);

        // no existing subscription
        _subRepo.FindAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        _subRepo.UpsertAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Subscription>().SubscriptionId);

        // Act
        var result = await _sut.ExecuteAsync(userId, isbn);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("テスト漫画", result.Value.SeriesTitle);
        await _subRepo.Received(1).UpsertAsync(Arg.Any<Subscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadySubscribed_ReturnsConflict()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var isbn = "9784000000001";
        var item = new RakutenBookSearchItem(isbn, "テスト漫画 1", "著者A", null, null, null, null);

        _rakutenSearch.SearchByIsbnAsync(isbn, Arg.Any<CancellationToken>())
            .Returns(item);
        _seriesRepo.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Series?)null);
        _seriesRepo.UpsertAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Series>().SeriesId);
        _volumeRepo.FindByIsbnAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Volume?)null);
        _volumeRepo.UpsertAsync(Arg.Any<Volume>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Volume>().VolumeId);
        _authorRepo.FindByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Author?)null);
        _authorRepo.UpsertAsync(Arg.Any<Author>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Author>().AuthorId);

        // existing active subscription
        var existingSub = Subscription.Create(userId, Guid.NewGuid());
        _subRepo.FindAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(existingSub);

        // Act
        var result = await _sut.ExecuteAsync(userId, isbn);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyExists", result.Error.Code);
    }
}
