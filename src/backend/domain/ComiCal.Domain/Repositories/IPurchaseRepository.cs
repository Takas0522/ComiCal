using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IPurchaseRepository
{
    Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Purchase?> FindAsync(Guid userId, Guid volumeId, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Purchase purchase, CancellationToken ct = default);
}
