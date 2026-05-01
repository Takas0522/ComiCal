using ComiCal.Domain.Entities;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class SeriesRepository(ComiCalDbContext db) : ISeriesRepository
{
    public Task<Series?> FindByIdAsync(Guid seriesId, CancellationToken ct = default)
        => db.Series
            .Include(s => s.Publisher)
            .Include(s => s.SeriesAuthors).ThenInclude(sa => sa.Author)
            .Include(s => s.Volumes).ThenInclude(v => v.ThumbnailAsset)
            .Where(s => !s.IsDeleted && s.SeriesId == seriesId)
            .FirstOrDefaultAsync(ct);

    public Task<Series?> FindByAggregateKeyAsync(string normalizedTitle, Guid primaryAuthorId, CancellationToken ct = default)
        => db.Series
            .Where(s => !s.IsDeleted && s.NormalizedTitle == normalizedTitle && s.PrimaryAuthorId == primaryAuthorId)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<Series> Items, string? NextCursor)> SearchAsync(
        SeriesSearchQuery query, CancellationToken ct = default)
    {
        var q = db.Series
            .Include(s => s.Publisher)
            .Include(s => s.SeriesAuthors).ThenInclude(sa => sa.Author)
            .Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            // FT インデックスは [NormalizedTitleHiragana] (PERSISTED computed column) に張られている。
            // CONTAINS は引数に関数式を取れないため、まず DB 側で `dbo.fnToHiragana(@q)` を評価し、
            // その結果をひらがな化済みのプレフィックス検索語 ("<hira>*") として CONTAINS に渡す。
            var sanitized = query.Q.Replace("\"", " ").Trim();
            var hiragana = await db.Database
                .SqlQuery<string>($"SELECT [dbo].[fnToHiragana]({sanitized}) AS Value")
                .FirstAsync(ct);

            if (!string.IsNullOrWhiteSpace(hiragana))
            {
                var ftSearchTerm = "\"" + hiragana + "*\"";
                q = q.Where(s => EF.Functions.Contains(
                    EF.Property<string>(s, "NormalizedTitleHiragana"), ftSearchTerm));
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Publisher))
        {
            q = q.Where(s => s.Publisher != null && s.Publisher.Name.Contains(query.Publisher));
        }

        // Keyset pagination on SeriesId
        if (query.Cursor is not null && Guid.TryParse(query.Cursor, out var cursor))
        {
            q = q.Where(s => s.SeriesId.CompareTo(cursor) > 0);
        }

        var items = await q.OrderBy(s => s.SeriesId)
            .Take(query.PageSize + 1)
            .ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > query.PageSize)
        {
            items = items[..query.PageSize];
            nextCursor = items[^1].SeriesId.ToString();
        }

        return (items, nextCursor);
    }

    public async Task<Guid> UpsertAsync(Series series, CancellationToken ct = default)
    {
        var existing = await db.Series.FindAsync([series.SeriesId], ct);
        if (existing is null)
            db.Series.Add(series);
        else
            db.Entry(existing).CurrentValues.SetValues(series);
        await db.SaveChangesAsync(ct);
        return series.SeriesId;
    }
}
