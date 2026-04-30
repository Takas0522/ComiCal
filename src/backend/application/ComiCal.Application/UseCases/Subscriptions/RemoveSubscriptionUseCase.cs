using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Subscriptions;

public sealed class RemoveSubscriptionUseCase(ISubscriptionRepository subRepo)
{
    public async Task<Result<bool>> ExecuteAsync(
        Guid userId, Guid seriesId, CancellationToken ct = default)
    {
        var sub = await subRepo.FindAsync(userId, seriesId, ct);
        if (sub is null || sub.IsDeleted) return Result.Failure<bool>(Error.NotFound("Subscription"));
        await subRepo.SoftDeleteAsync(sub.SubscriptionId, ct);
        return Result.Success(true);
    }
}
