using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class VolumeRepository(ComiCalDbContext db) : IVolumeRepository
{
    public Task<Volume?> FindByIdAsync(Guid volumeId, CancellationToken ct = default)
        => db.Volumes.Include(v => v.ThumbnailAsset)
            .Where(v => !v.IsDeleted && v.VolumeId == volumeId)
            .FirstOrDefaultAsync(ct);

    public Task<Volume?> FindByIsbnAsync(string isbn13, CancellationToken ct = default)
        => db.Volumes.Include(v => v.ThumbnailAsset)
            .Where(v => v.Isbn13 == isbn13)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<Volume> Items, string? NextCursor)> GetUpcomingAsync(
        UpcomingQuery query, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow.Date;
        var q = db.Volumes
            .Include(v => v.ThumbnailAsset)
            .Where(v => !v.IsDeleted && v.ReleaseDate.HasValue && v.ReleaseDate >= now);

        if (query.FilterBySeriesIds?.Count > 0)
            q = q.Where(v => query.FilterBySeriesIds.Contains(v.SeriesId));

        // Keyset pagination on (ReleaseDate, VolumeId)
        if (query.Cursor is not null)
        {
            var parts = query.Cursor.Split('_');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out var cursorDate) && Guid.TryParse(parts[1], out var cursorId))
            {
                q = q.Where(v => v.ReleaseDate > cursorDate ||
                                 (v.ReleaseDate == cursorDate && v.VolumeId.CompareTo(cursorId) > 0));
            }
        }

        var items = await q.OrderBy(v => v.ReleaseDate).ThenBy(v => v.VolumeId)
            .Take(query.PageSize + 1)
            .ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > query.PageSize)
        {
            items = items[..query.PageSize];
            var last = items[^1];
            nextCursor = $"{last.ReleaseDate:yyyy-MM-dd}_{last.VolumeId}";
        }

        return (items, nextCursor);
    }

    public async Task<IReadOnlyList<Volume>> GetCalendarAsync(
        CalendarQuery query, CancellationToken ct = default)
    {
        DateTime from, to;
        if (query.Week.HasValue)
        {
            // Calculate ISO week start (Monday)
            var jan4 = new DateTime(query.Year, 1, 4);
            var weekStart = jan4.AddDays((query.Week.Value - 1) * 7 - ((int)jan4.DayOfWeek + 6) % 7);
            from = weekStart;
            to = weekStart.AddDays(7);
        }
        else
        {
            from = new DateTime(query.Year, query.Month, 1);
            to = from.AddMonths(1);
        }

        var q = db.Volumes
            .Include(v => v.ThumbnailAsset)
            .Where(v => !v.IsDeleted && v.ReleaseDate.HasValue &&
                        v.ReleaseDate >= from && v.ReleaseDate < to);

        if (query.FilterBySeriesIds?.Count > 0)
            q = q.Where(v => query.FilterBySeriesIds.Contains(v.SeriesId));

        return await q.OrderBy(v => v.ReleaseDate).ThenBy(v => v.VolumeId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Volume>> GetBySeriesIdAsync(Guid seriesId, CancellationToken ct = default)
        => await db.Volumes.Include(v => v.ThumbnailAsset)
            .Where(v => !v.IsDeleted && v.SeriesId == seriesId)
            .OrderBy(v => v.VolumeNumber).ThenBy(v => v.ReleaseDate)
            .ToListAsync(ct);

    public async Task<Guid> UpsertAsync(Volume volume, CancellationToken ct = default)
    {
        var existing = await db.Volumes.FindAsync([volume.VolumeId], ct);
        if (existing is null)
            db.Volumes.Add(volume);
        else
            db.Entry(existing).CurrentValues.SetValues(volume);
        await db.SaveChangesAsync(ct);
        return volume.VolumeId;
    }
}
