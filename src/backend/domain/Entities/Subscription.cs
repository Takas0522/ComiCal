namespace ComiCal.Domain.Entities;

/// <summary>
/// 購読エンティティ（<c>dbo.Subscriptions</c>）。<c>(UserId, SeriesId)</c> をビジネスキーとして
/// 同一ユーザーが同一シリーズに対して複数の有効購読を持たないことを保証する。
/// 購読解除はソフト削除（<see cref="IsDeleted"/> = <c>true</c> + <see cref="DeletedAt"/>）。
/// </summary>
public sealed class Subscription
{
    /// <summary>購読 ID（PK、シーケンシャル GUID）。</summary>
    public Guid Id { get; private set; }

    /// <summary>購読ユーザー ID（<c>dbo.Users</c> への FK）。</summary>
    public Guid UserId { get; private set; }

    /// <summary>購読対象シリーズ ID（<c>dbo.Series</c> への FK）。</summary>
    public Guid SeriesId { get; private set; }

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時（UTC）。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private Subscription()
    {
    }

    /// <summary>新規購読のファクトリ。<paramref name="id"/> には sequential GUID を渡す。</summary>
    public static Subscription CreateNew(Guid id, Guid userId, Guid seriesId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Subscription id must not be empty.", nameof(id));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }
        if (seriesId == Guid.Empty)
        {
            throw new ArgumentException("SeriesId must not be empty.", nameof(seriesId));
        }

        return new Subscription
        {
            Id = id,
            UserId = userId,
            SeriesId = seriesId,
            IsDeleted = false,
            DeletedAt = null,
        };
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static Subscription Hydrate(
        Guid id,
        Guid userId,
        Guid seriesId,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Subscription
        {
            Id = id,
            UserId = userId,
            SeriesId = seriesId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }

    /// <summary>論理削除（購読解除）を行う。冪等。</summary>
    public void SoftDelete(DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return;
        }
        IsDeleted = true;
        DeletedAt = nowUtc;
    }

    /// <summary>ソフト削除済みエンティティを再有効化する（POST 冪等の resurrection 用）。</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}
