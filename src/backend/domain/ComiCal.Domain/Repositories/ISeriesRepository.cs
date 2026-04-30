using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;

namespace ComiCal.Domain.Repositories;

public interface ISeriesRepository
{
    Task<Series?> FindByIdAsync(Guid seriesId, CancellationToken ct = default);
    Task<Series?> FindByAggregateKeyAsync(string normalizedTitle, Guid primaryAuthorId, CancellationToken ct = default);
    Task<(IReadOnlyList<Series> Items, string? NextCursor)> SearchAsync(SeriesSearchQuery query, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Series series, CancellationToken ct = default);
}
