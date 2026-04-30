using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;

namespace ComiCal.Domain.Repositories;

public interface IVolumeRepository
{
    Task<Volume?> FindByIdAsync(Guid volumeId, CancellationToken ct = default);
    Task<Volume?> FindByIsbnAsync(string isbn13, CancellationToken ct = default);
    Task<(IReadOnlyList<Volume> Items, string? NextCursor)> GetUpcomingAsync(UpcomingQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<Volume>> GetCalendarAsync(CalendarQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<Volume>> GetBySeriesIdAsync(Guid seriesId, CancellationToken ct = default);
    Task<Guid> UpsertAsync(Volume volume, CancellationToken ct = default);
}
