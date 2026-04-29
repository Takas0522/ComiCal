namespace ComiCal.Domain.Entities;

/// <summary>
/// 購入エンティティ（<c>dbo.Purchases</c>）。<c>(UserId, VolumeId)</c> をビジネスキーとし、
/// DB レベルで一意制約を持つ。POST はこのキーで冪等 UPSERT。
/// </summary>
public sealed class Purchase
{
    /// <summary>購入 ID（PK、シーケンシャル GUID）。</summary>
    public Guid Id { get; private set; }

    /// <summary>購入したユーザー ID。</summary>
    public Guid UserId { get; private set; }

    /// <summary>購入した巻 ID。</summary>
    public Guid VolumeId { get; private set; }

    /// <summary>購入状態（<c>NotPurchased</c> / <c>Reserved</c> / <c>Purchased</c> / <c>Read</c>）。</summary>
    public string State { get; private set; } = "Purchased";

    /// <summary>購入日時（UTC、ユーザーが指定しなければサーバー時刻）。</summary>
    public DateTime? PurchasedAt { get; private set; }

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時（UTC）。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private Purchase()
    {
    }

    /// <summary>新規購入のファクトリ。</summary>
    public static Purchase CreateNew(Guid id, Guid userId, Guid volumeId, DateTime? purchasedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Purchase id must not be empty.", nameof(id));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }
        if (volumeId == Guid.Empty)
        {
            throw new ArgumentException("VolumeId must not be empty.", nameof(volumeId));
        }
        return new Purchase
        {
            Id = id,
            UserId = userId,
            VolumeId = volumeId,
            State = "Purchased",
            PurchasedAt = purchasedAt,
            IsDeleted = false,
            DeletedAt = null,
        };
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static Purchase Hydrate(
        Guid id,
        Guid userId,
        Guid volumeId,
        string state,
        DateTime? purchasedAt,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        return new Purchase
        {
            Id = id,
            UserId = userId,
            VolumeId = volumeId,
            State = state,
            PurchasedAt = purchasedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }

    /// <summary>UPSERT でサーバー側がすでに保有しているレコードのフィールドを更新する。</summary>
    public void UpdatePurchase(DateTime? purchasedAt)
    {
        State = "Purchased";
        PurchasedAt = purchasedAt;
        IsDeleted = false;
        DeletedAt = null;
    }

    /// <summary>論理削除を行う。冪等。</summary>
    public void SoftDelete(DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return;
        }
        IsDeleted = true;
        DeletedAt = nowUtc;
    }
}
