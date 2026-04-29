using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary><c>/api/me/*</c> ユースケース固有のエラーファクトリ。</summary>
internal static class MeErrors
{
    /// <summary>認証されていない（HTTP 401）。通常は SwaAuthMiddleware で先に弾かれるが、
    /// UseCase 単体テスト・防御的に検査するために提供。</summary>
    public static Error AuthRequired()
        => new(ErrorKind.Unauthorized, "unauthorized", "Authentication is required.");

    /// <summary>当該 (UserId, SeriesId) のアクティブな購読が存在しない（HTTP 404）。</summary>
    public static Error SubscriptionNotFound(Guid seriesId)
        => Error.NotFound("subscription-not-found", $"Subscription for series '{seriesId}' was not found.");

    /// <summary>当該 (UserId, VolumeId) のアクティブな購入が存在しない（HTTP 404）。</summary>
    public static Error PurchaseNotFound(Guid volumeId)
        => Error.NotFound("purchase-not-found", $"Purchase for volume '{volumeId}' was not found.");

    /// <summary>QR 同期トークンが見つからない（HTTP 404）。</summary>
    public static Error SyncTokenNotFound()
        => Error.NotFound("sync-token-not-found", "Sync token was not found.");

    /// <summary>QR 同期トークンが期限切れ（HTTP 410 Gone 相当だが、本実装では NotFound と同列に扱い 404 で返す）。</summary>
    public static Error SyncTokenExpired()
        => Error.NotFound("sync-token-expired", "Sync token has expired.");

    /// <summary>QR 同期トークンが既に消費済み（HTTP 409 Conflict）。</summary>
    public static Error SyncTokenAlreadyConsumed()
        => Error.Conflict("sync-token-already-consumed", "Sync token has already been consumed.");

    /// <summary>QR 同期トークンの所有者が現在のユーザーと一致しない（HTTP 403 Forbidden）。</summary>
    public static Error SyncTokenUserMismatch()
        => new(ErrorKind.Forbidden, "sync-token-user-mismatch", "Sync token does not belong to the authenticated user.");
}
