using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class RemoveSubscriptionUseCaseTests
{
    private readonly ISubscriptionRepository _repo = Substitute.For<ISubscriptionRepository>();
    private RemoveSubscriptionUseCase Sut => new(_repo);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new RemoveSubscriptionCommand(Guid.CreateVersion7()),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().SoftDeleteAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_validation_for_empty_series_id()
    {
        var userId = Guid.CreateVersion7();
        var result = await Sut.ExecuteAsync(
            new RemoveSubscriptionCommand(Guid.Empty),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_not_found_when_no_subscription_exists()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _repo.SoftDeleteAsync(userId, seriesId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.ExecuteAsync(
            new RemoveSubscriptionCommand(seriesId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("subscription-not-found");
    }

    [Fact]
    public async Task Returns_success_and_calls_repository_with_context_user_id()
    {
        var userId = Guid.CreateVersion7();
        var seriesId = Guid.CreateVersion7();
        _repo.SoftDeleteAsync(userId, seriesId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.ExecuteAsync(
            new RemoveSubscriptionCommand(seriesId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).SoftDeleteAsync(userId, seriesId, Arg.Any<CancellationToken>());
    }
}
