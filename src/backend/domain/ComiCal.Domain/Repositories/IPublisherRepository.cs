using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IPublisherRepository
{
    Task<Publisher?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Publisher publisher, CancellationToken ct = default);
}
