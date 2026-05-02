using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Purchases;

public sealed class UpdatePurchaseStateUseCase(
    IPurchaseRepository purchaseRepo,
    IVolumeRepository volumeRepo)
{
    public async Task<Result<bool>> ExecuteAsync(
        Guid userId, Guid volumeId, PurchaseState newState, CancellationToken ct = default)
    {
        var volume = await volumeRepo.FindByIdAsync(volumeId, ct);
        if (volume is null) return Result.Failure<bool>(Error.NotFound("Volume"));

        var existing = await purchaseRepo.FindAsync(userId, volumeId, ct);
        if (existing is null)
        {
            var newPurchase = Domain.Entities.Purchase.Create(userId, volumeId);
            newPurchase.UpdateState(newState);
            await purchaseRepo.UpsertAsync(newPurchase, ct);
        }
        else
        {
            existing.UpdateState(newState);
            await purchaseRepo.UpsertAsync(existing, ct);
        }
        return Result.Success(true);
    }
}
