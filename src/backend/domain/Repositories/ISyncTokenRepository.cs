using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

/// <summary>
/// QR 同期トークン（<see cref="SyncToken"/>）リポジトリ。Phase 2 端末間同期用。
/// </summary>
public interface ISyncTokenRepository
{
    /// <summary>新規発行されたトークンを永続化する。</summary>
    Task AddAsync(SyncToken token, CancellationToken cancellationToken);

    /// <summary>
    /// 指定の SHA-256 ハッシュに合致するアクティブトークン（未消費・未期限切れ）のみを返す。
    /// 存在しない／消費済み／期限切れの場合は <c>null</c>。
    /// </summary>
    /// <remarks>
    /// 失敗理由を区別したい呼び出し側（redeem ユースケース）は <see cref="FindByHashAsync"/> を使う。
    /// </remarks>
    Task<SyncToken?> GetActiveByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// 指定の SHA-256 ハッシュに合致するトークンを状態に関係なく返す（未消費／消費済／期限切れすべて）。
    /// 失敗種別（404 / 410 expired / 409 already-consumed / 403 user-mismatch）を区別する用途で利用する。
    /// </summary>
    Task<SyncToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// 指定 ID のトークンを「消費済み」(ConsumedAt = SYSUTCDATETIME()) でマークする。
    /// 既に消費済みの場合は冪等に <c>false</c> を返す。
    /// </summary>
    Task<bool> MarkConsumedAsync(Guid syncTokenId, CancellationToken cancellationToken);
}
