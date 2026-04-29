using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Application.Validators;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class AddSubscriptionUseCaseTests
{
    private readonly IValidator<AddSubscriptionCommand> _validator = new AddSubscriptionCommandValidator();
    private readonly ISubscriptionRepository _subs = Substitute.For<ISubscriptionRepository>();
    private readonly ISeriesRepository _series = Substitute.For<ISeriesRepository>();

    private AddSubscriptionUseCase Sut => new(_validator, _subs, _series);

    private static Series HydrateSeries(Guid id) => Series.Hydrate(
        id: id,
        title: "Test Series",
        normalizedTitle: "test-series",
        normalizedTitleHiragana: "てすとしりーず",
        publisherId: null,
        primaryAuthorId: Guid.CreateVersion7(),
        isCompleted: false,
        isDeleted: false,
        deletedAt: null,
        createdAt: DateTime.UtcNow,
        updatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new AddSubscriptionCommand(Guid.CreateVersion7()),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Returns_validation_for_empty_series_id()
    {
        var userId = Guid.CreateVersion7();

        var result = await Sut.ExecuteAsync(
            new AddSubscriptionCommand(Guid.Empty),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
        await _subs.DidNotReceiveWithAnyArgs().UpsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_not_found_when_series_does_not_exist()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _series.GetByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns((Series?)null);

        var result = await Sut.ExecuteAsync(
            new AddSubscriptionCommand(seriesId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("series-not-found");
        await _subs.DidNotReceiveWithAnyArgs().UpsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_created_true_for_new_subscription()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _series.GetByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(HydrateSeries(seriesId));

        var entity = Subscription.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, seriesId: seriesId,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

        _subs.UpsertAsync(userId, seriesId, Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Created));

        var result = await Sut.ExecuteAsync(
            new AddSubscriptionCommand(seriesId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeTrue();
        result.Value.Subscription.SeriesId.Should().Be(seriesId);
    }

    [Fact]
    public async Task Returns_created_false_when_subscription_already_exists()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _series.GetByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(HydrateSeries(seriesId));

        var entity = Subscription.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, seriesId: seriesId,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

        _subs.UpsertAsync(userId, seriesId, Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Existing));

        var result = await Sut.ExecuteAsync(
            new AddSubscriptionCommand(seriesId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeFalse();
    }

    [Fact]
    public async Task Use_case_uses_context_user_id_not_some_other_value()
    {
        var contextUserId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _series.GetByIdAsync(seriesId, Arg.Any<CancellationToken>()).Returns(HydrateSeries(seriesId));

        var entity = Subscription.Hydrate(
            id: Guid.CreateVersion7(), userId: contextUserId, seriesId: seriesId,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        _subs.UpsertAsync(contextUserId, seriesId, Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Created));

        await Sut.ExecuteAsync(
            new AddSubscriptionCommand(seriesId),
            new UseCaseContext(contextUserId, "c"),
            TestContext.Current.CancellationToken);

        await _subs.Received(1).UpsertAsync(contextUserId, seriesId, Arg.Any<CancellationToken>());
    }
}
