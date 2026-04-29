using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.Specifications;
using ComiCal.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVolumeRepository"/>.
/// Search uses SQL Server full-text <c>CONTAINS</c> (no <c>LIKE</c>) joining
/// the parent <c>Series</c>'s persisted hiragana column.
/// </summary>
public sealed class VolumeRepository : IVolumeRepository
{
    private readonly ComiCalDbContext _db;
    private readonly ILogger<VolumeRepository> _logger;

    public VolumeRepository(ComiCalDbContext db, ILogger<VolumeRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        _db = db;
        _logger = logger;
    }

    public async Task<Volume?> GetByIsbnAsync(Isbn13 isbn, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        return await _db.Volumes
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Isbn == isbn, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Volume?> GetByIdAsync(Guid volumeId, CancellationToken cancellationToken)
    {
        if (volumeId == Guid.Empty)
        {
            return null;
        }
        return await _db.Volumes
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == volumeId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Volume>> SearchAsync(
        VolumeSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IQueryable<Volume> query = _db.Volumes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            // Full-text CONTAINS against the Series.NormalizedTitleHiragana persisted column.
            // EF.Functions.Contains translates to SQL Server CONTAINS (no LIKE).
            var seriesIds = _db.Series
                .Where(s => EF.Functions.Contains(s.NormalizedTitleHiragana, criteria.Query))
                .Select(s => s.Id);
            query = query.Where(v => seriesIds.Contains(v.SeriesId));
        }

        if (criteria.ReleaseFrom is { } from)
        {
            query = query.Where(v => v.ReleaseDate >= from);
        }
        if (criteria.ReleaseTo is { } to)
        {
            query = query.Where(v => v.ReleaseDate <= to);
        }
        if (criteria.PublisherId is { } publisherId)
        {
            var seriesIdsByPublisher = _db.Series
                .Where(s => s.PublisherId == publisherId)
                .Select(s => s.Id);
            query = query.Where(v => seriesIdsByPublisher.Contains(v.SeriesId));
        }

        if (criteria.CursorReleaseDate is { } cursorDate && criteria.CursorVolumeId is { } cursorId)
        {
            query = query.Where(v =>
                v.ReleaseDate > cursorDate
                || (v.ReleaseDate == cursorDate && v.Id.CompareTo(cursorId) > 0));
        }

        return await query
            .OrderBy(v => v.ReleaseDate)
            .ThenBy(v => v.Id)
            .Take(criteria.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Volume>> GetByReleaseRangeAsync(
        DateOnly from,
        DateOnly to,
        int limit,
        Guid? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Volume> query = _db.Volumes
            .AsNoTracking()
            .Where(v => v.ReleaseDate >= from && v.ReleaseDate <= to);

        if (cursor is { } cursorId)
        {
            query = query.Where(v => v.Id.CompareTo(cursorId) > 0);
        }

        return await query
            .OrderBy(v => v.ReleaseDate)
            .ThenBy(v => v.Id)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Idempotent UPSERT keyed by ISBN-13. Implemented as a single
    /// <c>MERGE</c> statement so two batch workers can race safely.
    /// </summary>
    public async Task UpsertAsync(Volume volume, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(volume);

        const string sql = @"
MERGE dbo.Volumes WITH (HOLDLOCK) AS target
USING (SELECT @Isbn AS Isbn13) AS src
   ON target.Isbn13 = src.Isbn13
WHEN MATCHED THEN
    UPDATE SET
        SeriesId = @SeriesId,
        VolumeNumber = @VolumeNumber,
        ReleaseDate = @ReleaseDate,
        ReleaseDateIsMonthOnly = @ReleaseDateIsMonthOnly,
        CoverHash = @CoverHash,
        RakutenItemUrl = @RakutenItemUrl,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (VolumeId, SeriesId, Isbn13, VolumeNumber, ReleaseDate, ReleaseDateIsMonthOnly, CoverHash, RakutenItemUrl, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@VolumeId, @SeriesId, @Isbn, @VolumeNumber, @ReleaseDate, @ReleaseDateIsMonthOnly, @CoverHash, @RakutenItemUrl, 0, SYSUTCDATETIME(), SYSUTCDATETIME());";

        var parameters = new object[]
        {
            new SqlParameter("@VolumeId", volume.Id),
            new SqlParameter("@SeriesId", volume.SeriesId),
            new SqlParameter("@Isbn", volume.Isbn.Value),
            new SqlParameter("@VolumeNumber", (object?)volume.VolumeNumber ?? DBNull.Value),
            new SqlParameter("@ReleaseDate", (object?)volume.ReleaseDate ?? DBNull.Value),
            new SqlParameter("@ReleaseDateIsMonthOnly", volume.ReleaseDateIsMonthOnly),
            new SqlParameter("@CoverHash", volume.CoverHash.IsEmpty ? (object)DBNull.Value : volume.CoverHash.ToArray()),
            new SqlParameter("@RakutenItemUrl", (object?)volume.RakutenItemUrl ?? DBNull.Value),
        };

        _logger.LogDebug("Upserting volume {Isbn}", volume.Isbn.Value);
        await _db.Database
            .ExecuteSqlRawAsync(sql, parameters, cancellationToken)
            .ConfigureAwait(false);
    }
}
