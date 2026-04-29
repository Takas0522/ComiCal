using System.Security.Cryptography;

namespace ComiCal.Domain.Entities;

/// <summary>
/// 端末間 QR 同期トークンエンティティ（<c>dbo.SyncTokens</c>）。
/// 認証済みユーザーが「他端末からの同期意思」を一時的に表明するための
/// 短寿命（既定 5 分）ワンタイムトークン。
/// </summary>
/// <remarks>
/// <para>
/// プレーンテキストトークンは <see cref="CreateNew"/> 実行時に <see cref="RandomNumberGenerator"/>
/// で 256bit 生成され、Base64Url エンコードして発行端末側に一度だけ返却する。
/// サーバー側には SHA-256 ハッシュ（<see cref="TokenHash"/>）のみ永続化し、
/// 生トークンは決して保存・ログ出力しない。
/// </para>
/// <para>
/// 「消費済み」(<see cref="ConsumedAt"/> 非 null) または「期限切れ」(<see cref="ExpiresAt"/> 経過) の
/// レコードは事実上削除と同等に扱う。論理削除カラムは持たない。
/// </para>
/// </remarks>
public sealed class SyncToken
{
    /// <summary>SyncToken ID（PK、シーケンシャル GUID）。</summary>
    public Guid Id { get; private set; }

    /// <summary>発行元ユーザー ID（<c>dbo.Users</c> への FK）。</summary>
    public Guid UserId { get; private set; }

    /// <summary>プレーンテキストトークンの SHA-256 ハッシュ（32 byte）。</summary>
    public byte[] TokenHash { get; private set; } = Array.Empty<byte>();

    /// <summary>有効期限（UTC）。</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>消費（redeem）済み日時（UTC）。未消費は <c>null</c>。</summary>
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    private SyncToken()
    {
    }

    /// <summary>
    /// 新規 <see cref="SyncToken"/> を生成する。返り値の <c>PlaintextToken</c> は呼び出し元が
    /// QR ペイロードに埋め込んで一度だけ表示する責務を持ち、永続化してはならない。
    /// </summary>
    /// <param name="id">エンティティ PK（sequential GUID）。</param>
    /// <param name="userId">発行ユーザー ID。</param>
    /// <param name="nowUtc">基準時刻（UTC）。<see cref="CreatedAt"/> と <see cref="ExpiresAt"/> の起点。</param>
    /// <param name="ttl">有効期間。仕様の既定は 5 分。</param>
    public static (SyncToken Entity, string PlaintextToken) CreateNew(
        Guid id, Guid userId, DateTime nowUtc, TimeSpan ttl)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("SyncToken id must not be empty.", nameof(id));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");
        }

        var rng = RandomNumberGenerator.GetBytes(32);
        var plaintext = Base64UrlEncode(rng);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext));

        var entity = new SyncToken
        {
            Id = id,
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = nowUtc + ttl,
            ConsumedAt = null,
            CreatedAt = nowUtc,
        };
        return (entity, plaintext);
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static SyncToken Hydrate(
        Guid id, Guid userId, byte[] tokenHash,
        DateTime expiresAt, DateTime? consumedAt, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);
        return new SyncToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            ConsumedAt = consumedAt,
            CreatedAt = createdAt,
        };
    }

    /// <summary>消費済みフラグを立てる。冪等。</summary>
    public void MarkConsumed(DateTime nowUtc)
    {
        if (ConsumedAt is not null)
        {
            return;
        }
        ConsumedAt = nowUtc;
    }

    /// <summary>指定時刻時点でアクティブ（未消費かつ未期限切れ）かどうか。</summary>
    public bool IsActive(DateTime nowUtc) => ConsumedAt is null && ExpiresAt > nowUtc;

    /// <summary>プレーンテキストトークンに対応する SHA-256 ハッシュを計算する（リポジトリ検索用）。</summary>
    public static byte[] ComputeHash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
