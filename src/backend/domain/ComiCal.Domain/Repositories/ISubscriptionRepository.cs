using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface ISubscriptionRepository
{
    Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Subscription?> FindAsync(Guid userId, Guid seriesId, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Subscription subscription, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid subscriptionId, CancellationToken ct = default);
}
