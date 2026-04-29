using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISeriesRepository"/>.</summary>
public sealed class SeriesRepository : ISeriesRepository
{
    private readonly ComiCalDbContext _db;
    private readonly ILogger<SeriesRepository> _logger;

    public SeriesRepository(ComiCalDbContext db, ILogger<SeriesRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        _db = db;
        _logger = logger;
    }

    public async Task<Series?> GetByIdAsync(Guid seriesId, CancellationToken cancellationToken)
    {
        return await _db.Series
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Series>> SearchAsync(
        SeriesSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        IQueryable<Series> query = _db.Series.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            query = query.Where(s => EF.Functions.Contains(s.NormalizedTitleHiragana, criteria.Query));
        }
        if (criteria.PublisherId is { } publisherId)
        {
            query = query.Where(s => s.PublisherId == publisherId);
        }
        if (criteria.AuthorId is { } authorId)
        {
            query = query.Where(s => s.PrimaryAuthorId == authorId
                || s.Authors.Any(a => a.AuthorId == authorId));
        }
        if (criteria.CursorSeriesId is { } cursorId)
        {
            query = query.Where(s => s.Id.CompareTo(cursorId) > 0);
        }

        return await query
            .OrderBy(s => s.Id)
            .Take(criteria.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Series?> GetWithVolumesAsync(
        Guid seriesId,
        DateOnly? releaseFrom,
        CancellationToken cancellationToken)
    {
        var series = await _db.Series
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Volumes
                .Where(v => !v.IsDeleted && (releaseFrom == null || v.ReleaseDate >= releaseFrom)))
            .Include(s => s.Authors)
            .FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken)
            .ConfigureAwait(false);

        if (series is null)
        {
            _logger.LogDebug("Series {SeriesId} not found", seriesId);
        }
        return series;
    }
}
