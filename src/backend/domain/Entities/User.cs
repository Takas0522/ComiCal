namespace ComiCal.Domain.Entities;

/// <summary>
/// アプリケーション利用者（<c>dbo.Users</c>）。
/// <see cref="ExternalId"/> は IdP（Entra External ID）が発行する安定なサブジェクト識別子で、
/// 認証済みリクエストの解決キーとなる。<see cref="Id"/> はアプリ内部で生成する GUID PK。
/// </summary>
public sealed class User
{
    /// <summary>ユーザー ID（PK、シーケンシャル GUID）。</summary>
    public Guid Id { get; private set; }

    /// <summary>IdP の <c>sub</c>（SWA <c>userId</c>）。テナント横断で一意。</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>表示名（IdP 由来 / ユーザー編集可）。</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>ロール（<c>User</c> / <c>Admin</c>）。</summary>
    public string Role { get; private set; } = "User";

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private User()
    {
    }

    /// <summary>初回サインアップ時のファクトリ。<paramref name="id"/> には sequential GUID を渡す。</summary>
    public static User CreateNew(Guid id, string externalId, string displayName, string role = "User")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return new User
        {
            Id = id,
            ExternalId = externalId,
            DisplayName = displayName,
            Role = role,
            IsDeleted = false,
            DeletedAt = null,
            CreatedAt = default,
            UpdatedAt = default,
        };
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static User Hydrate(
        Guid id,
        string externalId,
        string displayName,
        string role,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        return new User
        {
            Id = id,
            ExternalId = externalId,
            DisplayName = displayName,
            Role = role,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }
}
