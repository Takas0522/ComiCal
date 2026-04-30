using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IBatchRunRepository
{
    Task<BatchRun?> FindByIdAsync(Guid batchRunId, CancellationToken ct = default);
    Task<(IReadOnlyList<BatchRun> Items, string? NextCursor)> GetAllAsync(string? cursor, int pageSize, CancellationToken ct = default);
    Task<Guid> CreateAsync(BatchRun batchRun, CancellationToken ct = default);
    Task UpdateAsync(BatchRun batchRun, CancellationToken ct = default);
    Task AddFailedItemAsync(FailedItem item, CancellationToken ct = default);
}
