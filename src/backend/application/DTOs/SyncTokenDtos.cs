namespace ComiCal.Application.DTOs;

/// <summary>
/// QR 同期トークン発行レスポンス。<c>POST /api/me/sync/qr</c> の成功時 200 ボディ。
/// </summary>
/// <param name="Token">プレーンテキストのワンタイムトークン（base64url、約 43 文字）。クライアントは
/// QR コードに埋め込んで一度だけ表示し、サーバー側の永続化はハッシュのみ。</param>
/// <param name="ExpiresAt">UTC の有効期限。</param>
/// <param name="QrPayload">QR エンコード対象の URL（<c>https://&lt;host&gt;/sync?token=&lt;token&gt;</c>）。</param>
public sealed record SyncTokenIssuedDto(string Token, DateTime ExpiresAt, string QrPayload);
