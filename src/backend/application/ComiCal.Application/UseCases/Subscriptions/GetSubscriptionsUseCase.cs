using ComiCal.Application.Dtos;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Subscriptions;

public sealed class GetSubscriptionsUseCase(ISubscriptionRepository subRepo)
{
    public async Task<Result<IReadOnlyList<SubscriptionDto>>> ExecuteAsync(
        Guid userId, CancellationToken ct = default)
    {
        var subs = await subRepo.GetByUserIdAsync(userId, ct);
        var dtos = subs.Select(s => new SubscriptionDto(
            s.SubscriptionId,
            s.SeriesId,
            s.Series?.Title ?? string.Empty,
            s.CreatedAt)).ToList();
        return Result.Success((IReadOnlyList<SubscriptionDto>)dtos);
    }
}
