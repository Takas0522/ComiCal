using ComiCal.Application.Common;
using ComiCal.Application.UseCases.Me;
using ComiCal.Application.Validators;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.ValueObjects;
using ComiCal.Shared;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Application.Me;

public sealed class AddPurchaseUseCaseTests
{
    private readonly IValidator<AddPurchaseCommand> _validator = new AddPurchaseCommandValidator();
    private readonly IPurchaseRepository _purchases = Substitute.For<IPurchaseRepository>();
    private readonly IVolumeRepository _volumes = Substitute.For<IVolumeRepository>();

    private AddPurchaseUseCase Sut => new(_validator, _purchases, _volumes);

    private static Volume HydrateVolume(Guid id) => Volume.Hydrate(
        id: id,
        seriesId: Guid.CreateVersion7(),
        isbn: Isbn13.Create("9784088100005"),
        volumeNumber: 1,
        releaseDate: new DateOnly(2026, 4, 1),
        releaseDateIsMonthOnly: false,
        coverHash: ReadOnlyMemory<byte>.Empty,
        rakutenItemUrl: null,
        isDeleted: false,
        deletedAt: null,
        createdAt: DateTime.UtcNow,
        updatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Returns_unauthorized_when_user_id_missing()
    {
        var result = await Sut.ExecuteAsync(
            new AddPurchaseCommand(Guid.CreateVersion7(), null),
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
            new AddPurchaseCommand(Guid.Empty, null),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_validation_for_future_purchased_at()
    {
        var userId = Guid.CreateVersion7();
        var result = await Sut.ExecuteAsync(
            new AddPurchaseCommand(Guid.CreateVersion7(), DateTime.UtcNow.AddDays(2)),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Validation);
    }

    [Fact]
    public async Task Returns_not_found_when_volume_does_not_exist()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _volumes.GetByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns((Volume?)null);

        var result = await Sut.ExecuteAsync(
            new AddPurchaseCommand(volumeId, null),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Code.Should().Be("volume-not-found");
        await _purchases.DidNotReceiveWithAnyArgs().UpsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_created_true_for_new_purchase()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _volumes.GetByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(HydrateVolume(volumeId));

        var entity = Purchase.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, volumeId: volumeId,
            state: "Purchased", purchasedAt: null,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        _purchases.UpsertAsync(userId, volumeId, null, Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Created));

        var result = await Sut.ExecuteAsync(
            new AddPurchaseCommand(volumeId, null),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeTrue();
        result.Value.Purchase.VolumeId.Should().Be(volumeId);
    }

    [Fact]
    public async Task Returns_created_false_when_purchase_already_existed_active()
    {
        var userId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _volumes.GetByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(HydrateVolume(volumeId));

        var entity = Purchase.Hydrate(
            id: Guid.CreateVersion7(), userId: userId, volumeId: volumeId,
            state: "Purchased", purchasedAt: null,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        _purchases.UpsertAsync(userId, volumeId, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Existing));

        var result = await Sut.ExecuteAsync(
            new AddPurchaseCommand(volumeId, null),
            new UseCaseContext(userId, "c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Created.Should().BeFalse();
    }

    [Fact]
    public async Task Use_case_uses_context_user_id_not_some_other_value()
    {
        var contextUserId = Guid.CreateVersion7();
        var volumeId = Guid.CreateVersion7();
        _volumes.GetByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(HydrateVolume(volumeId));

        var entity = Purchase.Hydrate(
            id: Guid.CreateVersion7(), userId: contextUserId, volumeId: volumeId,
            state: "Purchased", purchasedAt: null,
            isDeleted: false, deletedAt: null,
            createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        _purchases.UpsertAsync(contextUserId, volumeId, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns((entity, UpsertOutcome.Created));

        await Sut.ExecuteAsync(
            new AddPurchaseCommand(volumeId, null),
            new UseCaseContext(contextUserId, "c"),
            TestContext.Current.CancellationToken);

        await _purchases.Received(1).UpsertAsync(contextUserId, volumeId, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}
