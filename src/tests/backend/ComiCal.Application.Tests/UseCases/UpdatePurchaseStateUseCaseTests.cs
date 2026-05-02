using ComiCal.Application.UseCases.Purchases;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using NSubstitute;
using Xunit;

namespace ComiCal.Application.Tests.UseCases;

public sealed class UpdatePurchaseStateUseCaseTests
{
    private readonly IPurchaseRepository _purchaseRepo = Substitute.For<IPurchaseRepository>();
    private readonly IVolumeRepository _volumeRepo = Substitute.For<IVolumeRepository>();
    private readonly UpdatePurchaseStateUseCase _sut;

    public UpdatePurchaseStateUseCaseTests()
    {
        _sut = new UpdatePurchaseStateUseCase(_purchaseRepo, _volumeRepo);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVolumeNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var volumeId = Guid.NewGuid();
        _volumeRepo.FindByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns((Volume?)null);

        // Act
        var result = await _sut.ExecuteAsync(userId, volumeId, PurchaseState.Purchased);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoPurchaseExists_CreatesNewAndReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var volumeId = Guid.NewGuid();
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236", volumeNumber: 1);
        _volumeRepo.FindByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(volume);
        _purchaseRepo.FindAsync(userId, volumeId, Arg.Any<CancellationToken>()).Returns((Purchase?)null);
        _purchaseRepo.UpsertAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Purchase>().PurchaseId);

        // Act
        var result = await _sut.ExecuteAsync(userId, volumeId, PurchaseState.Purchased);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _purchaseRepo.Received(1).UpsertAsync(
            Arg.Is<Purchase>(p => p.State == PurchaseState.Purchased),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPurchaseExists_UpdatesStateAndReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var volumeId = Guid.NewGuid();
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236", volumeNumber: 1);
        var existingPurchase = Purchase.Create(userId, volumeId);
        _volumeRepo.FindByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(volume);
        _purchaseRepo.FindAsync(userId, volumeId, Arg.Any<CancellationToken>()).Returns(existingPurchase);
        _purchaseRepo.UpsertAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Purchase>().PurchaseId);

        // Act
        var result = await _sut.ExecuteAsync(userId, volumeId, PurchaseState.Reserved);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseState.Reserved, existingPurchase.State);
        await _purchaseRepo.Received(1).UpsertAsync(existingPurchase, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PurchaseState.NotPurchased)]
    [InlineData(PurchaseState.Reserved)]
    [InlineData(PurchaseState.Purchased)]
    [InlineData(PurchaseState.Read)]
    public async Task ExecuteAsync_AllPurchaseStates_ReturnsSuccess(PurchaseState state)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var volumeId = Guid.NewGuid();
        var volume = Volume.Create(Guid.NewGuid(), "9784088726236");
        _volumeRepo.FindByIdAsync(volumeId, Arg.Any<CancellationToken>()).Returns(volume);
        _purchaseRepo.FindAsync(userId, volumeId, Arg.Any<CancellationToken>()).Returns((Purchase?)null);
        _purchaseRepo.UpsertAsync(Arg.Any<Purchase>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Purchase>().PurchaseId);

        // Act
        var result = await _sut.ExecuteAsync(userId, volumeId, state);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
