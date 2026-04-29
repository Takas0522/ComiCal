using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

/// <summary>
/// 購入リポジトリ。<c>(UserId, VolumeId)</c> 一意制約に基づく冪等 UPSERT を提供する。
/// 取得系はデフォルトで論理削除済みを除外する。
/// </summary>
public interface IPurchaseRepository
{
    /// <summary>指定ユーザーのアクティブな購入を <c>CreatedAt</c> 昇順で返す（論理削除済みは除外）。</summary>
    Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>指定 <c>(UserId, VolumeId)</c> のレコードを取得する（論理削除済みも含む）。</summary>
    Task<Purchase?> FindAnyAsync(Guid userId, Guid volumeId, CancellationToken cancellationToken);

    /// <summary>
    /// 冪等 UPSERT。既存レコードがあれば <paramref name="purchasedAt"/> と <c>State="Purchased"</c> で更新し
    /// <see cref="UpsertOutcome.Existing"/> を返す。なければ新規挿入し <see cref="UpsertOutcome.Created"/>。
    /// 既存がソフト削除済みの場合は復活させて <see cref="UpsertOutcome.Created"/> を返す。
    /// </summary>
    Task<(Purchase Entity, UpsertOutcome Outcome)> UpsertAsync(
        Guid userId,
        Guid volumeId,
        DateTime? purchasedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// 指定 <c>(UserId, VolumeId)</c> のアクティブな購入をソフト削除する。
    /// 該当レコードが存在しない場合は <c>false</c> を返す。
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid userId, Guid volumeId, CancellationToken cancellationToken);
}
