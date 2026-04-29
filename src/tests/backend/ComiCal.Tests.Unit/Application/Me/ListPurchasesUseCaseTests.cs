using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.UseCases.Me;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class ListPurchasesUseCaseTests
{
    private readonly IPurchaseRepository _repo = Substitute.For<IPurchaseRepository>();
    private ListPurchasesUseCase Sut => new(_repo);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new ListPurchasesQuery(),
            new UseCaseContext(UserId: null, CorrelationId: "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Maps_repository_results_to_dto_list()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        var purchase = Purchase.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, volumeId: volumeId,
            state: "Purchased", purchasedAt: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);

        _repo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new[] { purchase });

        var result = await Sut.ExecuteAsync(
            new ListPurchasesQuery(),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Should().ContainSingle().Subject;
        item.VolumeId.Should().Be(volumeId);
        item.State.Should().Be("Purchased");
        ((PurchaseDto)item).Should().NotBeNull();
    }
}

