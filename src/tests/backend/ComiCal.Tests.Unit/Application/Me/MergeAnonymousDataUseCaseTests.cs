using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases.Me;
using ComiCal.Application.Validators;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.ValueObjects;
using ComiCal.Shared;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class MergeAnonymousDataUseCaseTests
{
    private readonly IValidator<MergeAnonymousDataCommand> _validator = new MergeAnonymousDataCommandValidator();
    private readonly ISubscriptionRepository _subs = Substitute.For<ISubscriptionRepository>();
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly ISeriesRepository _series = Substitute.For<ISeriesRepository>();
    private readonly IVolumeRepository _volumes = Substitute.For<IVolumeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public MergeAnonymousDataUseCaseTests()
    {
        // Pass-through transaction (no real DB).
        _uow.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<MergeResultDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var fn = call.Arg<Func<CancellationToken, Task<MergeResultDto>>>();
                var ct = call.Arg<CancellationToken>();
                return fn(ct);
            });
    }

    private MergeAnonymousDataUseCase Sut => new(_validator, _subs, _purchases, _series, _volumes, _uow);

    private static Series HydrateSeries(Guid id) => Series.Hydrate(
        id: id, title: "T", normalizedTitle: "t", normalizedTitleHiragana: "t",
        publisherId: null, primaryAuthorId: Guid.CreateVersion7(),
        isCompleted: false, isDeleted: false, deletedAt: null,
        createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

    private static Volume HydrateVolume(Guid id) => Volume.Hydrate(
        id: id, seriesId: Guid.CreateVersion7(),
        isbn: Isbn13.Create("9784088100005"),
        volumeNumber: 1, releaseDate: new DateOnly(2026, 4, 1),
        releaseDateIsMonthOnly: false, coverHash: ReadOnlyMemory<byte>.Empty,
        rakutenItemUrl: null, isDeleted: false, deletedAt: null,
        createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

    private static Subscription HydrateSubscription(Guid userId, Guid seriesId) =>
        Subscription.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, seriesId: seriesId,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

    private static Purchase HydratePurchase(Guid userId, Guid volumeId) =>
        Purchase.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, volumeId: volumeId,
            state: "Purchased", purchasedAt: DateTime.UtcNow,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                Array.Empty<MergeAnonymousSubscriptionItem>(),
                Array.Empty<MergeAnonymousPurchaseItem>()),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Empty_payload_succeeds_as_noop()
    {
        var userId = Guid.CreateVersion7();

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                Array.Empty<MergeAnonymousSubscriptionItem>(),
                Array.Empty<MergeAnonymousPurchaseItem>()),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Merged.Subscriptions.Should().Be(0);
        result.Value.Merged.Purchases.Should().Be(0);
        result.Value.Skipped.Subscriptions.Should().BeEmpty();
        result.Value.Skipped.Purchases.Should().BeEmpty();
        await _uow.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<MergeResultDto>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task All_valid_items_are_upserted_inside_transaction()
    {
        var userId = Guid.CreateVersion7();
        var s1 = Guid.CreateVersion7();
        var s2 = Guid.CreateVersion7();
        var v1 = Guid.CreateVersion7();
        _series.GetByIdAsync(s1, Arg.Any<CancellationToken>()).Returns(HydrateSeries(s1));
        _series.GetByIdAsync(s2, Arg.Any<CancellationToken>()).Returns(HydrateSeries(s2));
        _volumes.GetByIdAsync(v1, Arg.Any<CancellationToken>()).Returns(HydrateVolume(v1));
        _subs.UpsertAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => (HydrateSubscription(userId, (Guid)call[1]), UpsertOutcome.Created));
        _purchases.UpsertAsync(userId, Arg.Any<Guid>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(call => (HydratePurchase(userId, (Guid)call[1]), UpsertOutcome.Created));

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                new[] { new MergeAnonymousSubscriptionItem(s1), new MergeAnonymousSubscriptionItem(s2) },
                new[] { new MergeAnonymousPurchaseItem(v1, DateTime.UtcNow.AddDays(-1)) }),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Merged.Subscriptions.Should().Be(2);
        result.Value.Merged.Purchases.Should().Be(1);
        result.Value.Skipped.Subscriptions.Should().BeEmpty();
        result.Value.Skipped.Purchases.Should().BeEmpty();

        await _uow.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<MergeResultDto>>>(),
            Arg.Any<CancellationToken>());
        await _subs.Received(1).UpsertAsync(userId, s1, Arg.Any<CancellationToken>());
        await _subs.Received(1).UpsertAsync(userId, s2, Arg.Any<CancellationToken>());
        await _purchases.Received(1).UpsertAsync(userId, v1, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Items_pointing_to_missing_targets_are_skipped_not_failed()
    {
        var userId = Guid.CreateVersion7();
        var goodSeries = Guid.CreateVersion7();
        var badSeries = Guid.CreateVersion7();
        var goodVolume = Guid.CreateVersion7();
        var badVolume = Guid.CreateVersion7();
        _series.GetByIdAsync(goodSeries, Arg.Any<CancellationToken>()).Returns(HydrateSeries(goodSeries));
        _series.GetByIdAsync(badSeries, Arg.Any<CancellationToken>()).Returns((Series?)null);
        _volumes.GetByIdAsync(goodVolume, Arg.Any<CancellationToken>()).Returns(HydrateVolume(goodVolume));
        _volumes.GetByIdAsync(badVolume, Arg.Any<CancellationToken>()).Returns((Volume?)null);
        _subs.UpsertAsync(userId, goodSeries, Arg.Any<CancellationToken>())
            .Returns((HydrateSubscription(userId, goodSeries), UpsertOutcome.Created));
        _purchases.UpsertAsync(userId, goodVolume, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns((HydratePurchase(userId, goodVolume), UpsertOutcome.Created));

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                new[] { new MergeAnonymousSubscriptionItem(goodSeries), new MergeAnonymousSubscriptionItem(badSeries) },
                new[] { new MergeAnonymousPurchaseItem(goodVolume, null), new MergeAnonymousPurchaseItem(badVolume, null) }),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Merged.Subscriptions.Should().Be(1);
        result.Value.Merged.Purchases.Should().Be(1);
        result.Value.Skipped.Subscriptions.Should().ContainSingle().Which.Should().Be(badSeries);
        result.Value.Skipped.Purchases.Should().ContainSingle().Which.Should().Be(badVolume);

        await _subs.DidNotReceive().UpsertAsync(userId, badSeries, Arg.Any<CancellationToken>());
        await _purchases.DidNotReceive().UpsertAsync(userId, badVolume, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_ids_in_payload_are_deduplicated()
    {
        var userId = Guid.CreateVersion7();
        var s1 = Guid.CreateVersion7();
        _series.GetByIdAsync(s1, Arg.Any<CancellationToken>()).Returns(HydrateSeries(s1));
        _subs.UpsertAsync(userId, s1, Arg.Any<CancellationToken>())
            .Returns((HydrateSubscription(userId, s1), UpsertOutcome.Created));

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                new[] { new MergeAnonymousSubscriptionItem(s1), new MergeAnonymousSubscriptionItem(s1) },
                Array.Empty<MergeAnonymousPurchaseItem>()),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Merged.Subscriptions.Should().Be(1);
        await _subs.Received(1).UpsertAsync(userId, s1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Repository_exception_returns_unexpected_failure()
    {
        var userId = Guid.CreateVersion7();
        var s1 = Guid.CreateVersion7();
        _series.GetByIdAsync(s1, Arg.Any<CancellationToken>()).Returns(HydrateSeries(s1));
        _subs.UpsertAsync(userId, s1, Arg.Any<CancellationToken>())
            .ThrowsAsyncForAnyArgs(new InvalidOperationException("DB exploded"));

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(
                new[] { new MergeAnonymousSubscriptionItem(s1) },
                Array.Empty<MergeAnonymousPurchaseItem>()),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unexpected);
        result.Error.Code.Should().Be("merge-failed");
    }

    [Fact]
    public async Task Oversized_subscription_payload_fails_validation()
    {
        var userId = Guid.CreateVersion7();
        var subs = Enumerable.Range(0, MergeAnonymousDataCommandValidator.MaxSubscriptions + 1)
            .Select(_ => new MergeAnonymousSubscriptionItem(Guid.CreateVersion7()))
            .ToArray();

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(subs, Array.Empty<MergeAnonymousPurchaseItem>()),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        await _uow.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<MergeResultDto>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Oversized_purchase_payload_fails_validation()
    {
        var userId = Guid.CreateVersion7();
        var purchases = Enumerable.Range(0, MergeAnonymousDataCommandValidator.MaxPurchases + 1)
            .Select(_ => new MergeAnonymousPurchaseItem(Guid.CreateVersion7(), null))
            .ToArray();

        var result = await Sut.ExecuteAsync(
            new MergeAnonymousDataCommand(Array.Empty<MergeAnonymousSubscriptionItem>(), purchases),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }
}
