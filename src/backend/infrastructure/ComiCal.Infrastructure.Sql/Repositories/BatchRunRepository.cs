using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class BatchRunRepository(ComiCalDbContext db) : IBatchRunRepository
{
    public Task<BatchRun?> FindByIdAsync(Guid batchRunId, CancellationToken ct = default)
        => db.BatchRuns.Include(b => b.FailedItems)
            .FirstOrDefaultAsync(b => b.BatchRunId == batchRunId, ct);

    public async Task<(IReadOnlyList<BatchRun> Items, string? NextCursor)> GetAllAsync(
        string? cursor, int pageSize, CancellationToken ct = default)
    {
        var q = db.BatchRuns.AsQueryable();
        if (cursor is not null && DateTime.TryParse(cursor, out var cursorDate))
            q = q.Where(b => b.StartedAt < cursorDate);

        var items = await q.OrderByDescending(b => b.StartedAt)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > pageSize)
        {
            items = items[..pageSize];
            nextCursor = items[^1].StartedAt.ToString("O");
        }

        return (items, nextCursor);
    }

    public async Task<Guid> CreateAsync(BatchRun batchRun, CancellationToken ct = default)
    {
        db.BatchRuns.Add(batchRun);
        await db.SaveChangesAsync(ct);
        return batchRun.BatchRunId;
    }

    public async Task UpdateAsync(BatchRun batchRun, CancellationToken ct = default)
    {
        db.Entry(batchRun).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task AddFailedItemAsync(FailedItem item, CancellationToken ct = default)
    {
        db.FailedItems.Add(item);
        await db.SaveChangesAsync(ct);
    }
}
