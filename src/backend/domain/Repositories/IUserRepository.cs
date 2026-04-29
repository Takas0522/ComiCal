using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

/// <summary>
/// アプリケーション利用者（<c>dbo.Users</c>）リポジトリ。Phase 2 で SWA 認証から
/// 解決されたプリンシパルを内部 <see cref="User"/> 集約にマッピングする。
/// </summary>
public interface IUserRepository
{
    /// <summary>IdP サブジェクト（<see cref="User.ExternalId"/>）でユーザーを取得する。論理削除済みは除外。</summary>
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);

    /// <summary>
    /// <paramref name="externalId"/> に対応するユーザーを保証する UPSERT。
    /// 既存があればそのまま返し、無ければ新規 sequential GUID で挿入する。
    /// 同一 <paramref name="externalId"/> で複数回呼んでも冪等に同じ <see cref="User.Id"/> を返す。
    /// </summary>
    /// <param name="externalId">IdP の <c>sub</c>（SWA <c>userId</c>）。</param>
    /// <param name="displayName">新規挿入時に使用する表示名。</param>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    Task<User> EnsureExistsAsync(
        string externalId,
        string displayName,
        CancellationToken cancellationToken);

    /// <summary>
    /// 利用者本人によるアカウント削除（個人情報保護法準拠のハード削除）。
    /// <c>Users</c> 行と FK 子テーブル（<c>Subscriptions</c> / <c>Purchases</c> /
    /// <c>IdentityLinks</c>）を **物理削除** する。冪等: 対象が存在しない場合は何もせず
    /// <see langword="false"/> を返す。<see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/>
    /// で原子性を保証する。
    /// </summary>
    /// <param name="userId">対象ユーザー ID（PK）。</param>
    /// <param name="cancellationToken">キャンセル トークン。</param>
    /// <returns>削除対象が存在し物理削除した場合 <see langword="true"/>、既に存在しない場合 <see langword="false"/>。</returns>
    Task<bool> HardDeleteAsync(Guid userId, CancellationToken cancellationToken);
}
