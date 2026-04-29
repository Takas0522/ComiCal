using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class RemovePurchaseUseCaseTests
{
    private readonly IPurchaseRepository _repo = Substitute.For<IPurchaseRepository>();
    private RemovePurchaseUseCase Sut => new(_repo);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new RemovePurchaseCommand(Guid.CreateVersion7()),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Returns_validation_for_empty_volume_id()
    {
        var userId = Guid.CreateVersion7();
        var result = await Sut.ExecuteAsync(
            new RemovePurchaseCommand(Guid.Empty),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_not_found_when_no_purchase_exists()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _repo.SoftDeleteAsync(userId, volumeId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut.ExecuteAsync(
            new RemovePurchaseCommand(volumeId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("purchase-not-found");
    }

    [Fact]
    public async Task Returns_success_and_calls_repository_with_context_user_id()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _repo.SoftDeleteAsync(userId, volumeId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut.ExecuteAsync(
            new RemovePurchaseCommand(volumeId),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).SoftDeleteAsync(userId, volumeId, Arg.Any<CancellationToken>());
    }
}
