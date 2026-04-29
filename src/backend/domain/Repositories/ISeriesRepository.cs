using ComiCal.Domain.Entities;
using ComiCal.Domain.Specifications;

namespace ComiCal.Domain.Repositories;

/// <summary>シリーズリポジトリ。Phase 1 では参照系のみ。</summary>
public interface ISeriesRepository
{
    /// <summary>シリーズ ID で 1 件取得する（巻は含まない）。論理削除済みは除外。</summary>
    Task<Series?> GetByIdAsync(Guid seriesId, CancellationToken cancellationToken);

    /// <summary>条件指定でシリーズを検索する（keyset pagination）。論理削除済みは除外。</summary>
    Task<IReadOnlyList<Series>> SearchAsync(SeriesSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// シリーズと、指定日以降に発売される巻（<paramref name="releaseFrom"/> が <c>null</c> なら全巻）を一括で取得する。
    /// </summary>
    Task<Series?> GetWithVolumesAsync(Guid seriesId, DateOnly? releaseFrom, CancellationToken cancellationToken);
}
