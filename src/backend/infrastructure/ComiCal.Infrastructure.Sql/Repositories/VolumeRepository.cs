using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

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
            .Include(v => v.Series)
            .Where(v => !v.IsDeleted && v.Series != null && !v.Series.IsDeleted &&
                        v.ReleaseDate.HasValue && v.ReleaseDate >= now);

        if (query.FilterBySeriesIds?.Count > 0)
            q = q.Where(v => query.FilterBySeriesIds.Contains(v.SeriesId));

        q = await ApplyKeywordFilterAsync(q, query.Keywords, ct);

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
        if (query.FromDate.HasValue && query.ToDate.HasValue)
        {
            from = query.FromDate.Value.Date;
            to = query.ToDate.Value.Date;
        }
        else if (query.Week.HasValue)
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
            .Include(v => v.Series)
            .Where(v => !v.IsDeleted && v.Series != null && !v.Series.IsDeleted);

        if (query.FilterBySeriesIds?.Count > 0)
            q = q.Where(v => query.FilterBySeriesIds.Contains(v.SeriesId));

        if (query.Keywords?.Count > 0)
        {
            q = q.Where(v => !v.ReleaseDate.HasValue ||
                             (v.ReleaseDate >= from && v.ReleaseDate < to));
            q = await ApplyKeywordFilterAsync(q, query.Keywords, ct);
        }
        else
        {
            q = q.Where(v => v.ReleaseDate.HasValue &&
                             v.ReleaseDate >= from && v.ReleaseDate < to);
        }

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

    private async Task<IQueryable<Volume>> ApplyKeywordFilterAsync(
        IQueryable<Volume> volumes,
        IReadOnlyList<string>? keywords,
        CancellationToken ct)
    {
        if (keywords?.Count is not > 0)
            return volumes;

        var sanitizedKeywords = keywords
            .Select(SanitizeFullTextPhrase)
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (sanitizedKeywords.Length == 0)
            return volumes.Where(_ => false);

        // OPENJSON binds the serialized array as one parameter, so all terms are normalized in one round trip.
        var normalizedKeywordsJson = JsonSerializer.Serialize(sanitizedKeywords);
        var terms = (await db.Database
            .SqlQuery<string>($"""
                SELECT [dbo].[fnToHiragana]([value]) AS [Value]
                FROM OPENJSON({normalizedKeywordsJson})
                WHERE [type] = 1
                """)
            .ToListAsync(ct))
            .Where(hiragana => !string.IsNullOrWhiteSpace(hiragana))
            .Select(hiragana => $"\"{hiragana}*\"")
            .ToArray();

        if (terms.Length == 0)
            return volumes.Where(_ => false);

        Expression<Func<Series, bool>>? matchesAnyTerm = null;
        foreach (var term in terms)
        {
            Expression<Func<Series, bool>> matchesTerm = series =>
                EF.Functions.Contains(
                    EF.Property<string>(series, "NormalizedTitleHiragana"),
                    term) ||
                series.SeriesAuthors.Any(seriesAuthor =>
                    seriesAuthor.Author != null &&
                    !seriesAuthor.Author.IsDeleted &&
                    EF.Functions.Contains(
                        EF.Property<string>(seriesAuthor.Author, "NormalizedNameHiragana"),
                        term));

            matchesAnyTerm = matchesAnyTerm is null
                ? matchesTerm
                : Or(matchesAnyTerm, matchesTerm);
        }

        var matchingSeriesIds = db.Series
            .AsNoTracking()
            .Where(series => !series.IsDeleted)
            .Where(matchesAnyTerm!)
            .Select(series => series.SeriesId);

        return volumes.Where(volume => matchingSeriesIds.Contains(volume.SeriesId));
    }

    private static string SanitizeFullTextPhrase(string keyword)
        => string.Concat(keyword.Select(character =>
            char.IsLetterOrDigit(character) || char.IsWhiteSpace(character) ? character : ' ')).Trim();

    private static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!,
            new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private sealed class ReplaceParameterVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == source ? target : base.VisitParameter(node);
    }
}
