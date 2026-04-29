using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class ListSubscriptionsUseCaseTests
{
    private readonly ISubscriptionRepository _repo = Substitute.For<ISubscriptionRepository>();

    private ListSubscriptionsUseCase Sut => new(_repo);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_is_missing()
    {
        var result = await Sut.ExecuteAsync(
            new ListSubscriptionsQuery(),
            new UseCaseContext(UserId: null, CorrelationId: "c1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_unauthorized_when_user_id_is_empty_guid()
    {
        var result = await Sut.ExecuteAsync(
            new ListSubscriptionsQuery(),
            new UseCaseContext(UserId: Guid.Empty, CorrelationId: "c1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Maps_repository_results_to_dto_list()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        var sub = Subscription.Hydrate(
            id: Guid.CreateVersion7(),
            userId: userId,
            seriesId: seriesId,
            isDeleted: false,
            deletedAt: null,
            createdAt: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            updatedAt: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { sub });

        var result = await Sut.ExecuteAsync(
            new ListSubscriptionsQuery(),
            new UseCaseContext(userId, "c1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].SeriesId.Should().Be(seriesId);
    }

    [Fact]
    public async Task Empty_repository_returns_empty_dto_list()
    {
        var userId = Guid.CreateVersion7();
        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Subscription>());

        var result = await Sut.ExecuteAsync(
            new ListSubscriptionsQuery(),
            new UseCaseContext(userId, "c1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
