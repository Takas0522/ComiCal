using ComiCal.Domain.Entities;
using ComiCal.Domain.Specifications;
using ComiCal.Domain.ValueObjects;

namespace ComiCal.Domain.Repositories;

/// <summary>巻リポジトリ。読込操作（Phase 1）と UPSERT（Phase 2 バッチ）を提供する。</summary>
public interface IVolumeRepository
{
    /// <summary>ISBN-13 で 1 件取得する。論理削除済みは除外。</summary>
    Task<Volume?> GetByIsbnAsync(Isbn13 isbn, CancellationToken cancellationToken);

    /// <summary>内部 ID（GUID）で 1 件取得する。論理削除済みは除外。</summary>
    Task<Volume?> GetByIdAsync(Guid volumeId, CancellationToken cancellationToken);

    /// <summary>条件指定で巻を検索する（keyset pagination）。論理削除済みは除外。</summary>
    Task<IReadOnlyList<Volume>> SearchAsync(VolumeSearchCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// 発売日範囲で巻を取得する（カレンダー / 直近発売予定用）。
    /// keyset pagination のカーソルは <c>(ReleaseDate, VolumeId)</c>。<paramref name="cursor"/> は前ページ末尾の VolumeId。
    /// </summary>
    Task<IReadOnlyList<Volume>> GetByReleaseRangeAsync(
        DateOnly from,
        DateOnly to,
        int limit,
        Guid? cursor,
        CancellationToken cancellationToken);

    /// <summary>巻を登録または更新する（ISBN-13 を一意キー）。バッチで利用。</summary>
    Task UpsertAsync(Volume volume, CancellationToken cancellationToken);
}
