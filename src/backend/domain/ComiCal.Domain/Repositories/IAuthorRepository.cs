using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IAuthorRepository
{
    Task<Author?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Author author, CancellationToken ct = default);
}
