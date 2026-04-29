using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

/// <summary>
/// 購読リポジトリ。Phase 2 認証済みユーザー向け CRUD を提供する。
/// 取得系はデフォルトで論理削除済みを除外する。
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>指定ユーザーのアクティブな購読を <c>CreatedAt</c> 昇順で返す（論理削除済みは除外）。</summary>
    Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>指定 <c>(UserId, SeriesId)</c> のレコードを取得する（論理削除済みも含む）。存在しない場合は <c>null</c>。</summary>
    Task<Subscription?> FindAnyAsync(Guid userId, Guid seriesId, CancellationToken cancellationToken);

    /// <summary>
    /// 冪等 UPSERT。<paramref name="userId"/> と <paramref name="seriesId"/> の組み合わせで
    /// 既存のアクティブな購読がある場合は <see cref="UpsertOutcome.Existing"/> を返す。
    /// 既存のソフト削除レコードがある場合は再有効化して <see cref="UpsertOutcome.Created"/> を返す。
    /// 何もなければ新規 sequential GUID で挿入し <see cref="UpsertOutcome.Created"/> を返す。
    /// </summary>
    Task<(Subscription Entity, UpsertOutcome Outcome)> UpsertAsync(
        Guid userId,
        Guid seriesId,
        CancellationToken cancellationToken);

    /// <summary>
    /// 指定 <c>(UserId, SeriesId)</c> のアクティブな購読をソフト削除する。
    /// 該当レコードが存在しない場合は <c>false</c> を返す（呼び出し側で 404 か 204 かを判断する）。
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid userId, Guid seriesId, CancellationToken cancellationToken);
}

/// <summary>UPSERT 結果（<c>Created</c> = 新規挿入 or 復活、<c>Existing</c> = 既存アクティブ）。</summary>
public enum UpsertOutcome
{
    Existing = 0,
    Created = 1,
}
