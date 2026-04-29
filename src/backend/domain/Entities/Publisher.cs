namespace ComiCal.Domain.Entities;

/// <summary>出版社エンティティ（<c>dbo.Publishers</c>）。</summary>
public sealed class Publisher
{
    /// <summary>出版社 ID（PK）。</summary>
    public Guid Id { get; private set; }

    /// <summary>表示名。</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>正規化名。</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>ひらがな正規化済み名（PERSISTED 計算列の写像）。</summary>
    public string NormalizedNameHiragana { get; private set; } = string.Empty;

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private Publisher()
    {
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static Publisher Hydrate(
        Guid id,
        string name,
        string normalizedName,
        string normalizedNameHiragana,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(normalizedName);
        ArgumentNullException.ThrowIfNull(normalizedNameHiragana);
        return new Publisher
        {
            Id = id,
            Name = name,
            NormalizedName = normalizedName,
            NormalizedNameHiragana = normalizedNameHiragana,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }
}
