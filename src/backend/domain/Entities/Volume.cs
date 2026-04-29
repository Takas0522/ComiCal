using ComiCal.Domain.ValueObjects;

namespace ComiCal.Domain.Entities;

/// <summary>
/// 巻エンティティ（<c>dbo.Volumes</c>）。ISBN-13 を主軸キーとする。
/// </summary>
public sealed class Volume
{
    /// <summary>巻 ID（PK、内部）。</summary>
    public Guid Id { get; private set; }

    /// <summary>所属シリーズ ID。</summary>
    public Guid SeriesId { get; private set; }

    /// <summary>ISBN-13。</summary>
    public Isbn13 Isbn { get; private set; } = default!;

    /// <summary>巻数（抽出失敗時は <c>null</c>）。</summary>
    public int? VolumeNumber { get; private set; }

    /// <summary>
    /// 発売日。月のみ判明時は当該月末日 (<see cref="ReleaseDateIsMonthOnly"/> = <c>true</c>)、未定時は <c>null</c>。
    /// </summary>
    public DateOnly? ReleaseDate { get; private set; }

    /// <summary>発売日が月のみ判明状態であるかどうか。</summary>
    public bool ReleaseDateIsMonthOnly { get; private set; }

    /// <summary>表紙画像の SHA-256 ハッシュ（差分検出用）。</summary>
    public ReadOnlyMemory<byte> CoverHash { get; private set; }

    /// <summary>楽天 Books の商品ページ URL（アフィリエイトリンク用）。</summary>
    public string? RakutenItemUrl { get; private set; }

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private Volume()
    {
    }

    /// <summary>新規巻を生成する（バッチ取込時の UPSERT 経路で利用想定）。</summary>
    public static Volume Create(
        Guid seriesId,
        Isbn13 isbn,
        int? volumeNumber,
        DateOnly? releaseDate,
        bool releaseDateIsMonthOnly,
        ReadOnlyMemory<byte> coverHash,
        string? rakutenItemUrl)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        if (seriesId == Guid.Empty)
        {
            throw new ArgumentException("SeriesId must not be empty.", nameof(seriesId));
        }
        var now = DateTime.UtcNow;
        return new Volume
        {
            Id = Guid.CreateVersion7(),
            SeriesId = seriesId,
            Isbn = isbn,
            VolumeNumber = volumeNumber,
            ReleaseDate = releaseDate,
            ReleaseDateIsMonthOnly = releaseDateIsMonthOnly,
            CoverHash = coverHash,
            RakutenItemUrl = rakutenItemUrl,
            IsDeleted = false,
            DeletedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static Volume Hydrate(
        Guid id,
        Guid seriesId,
        Isbn13 isbn,
        int? volumeNumber,
        DateOnly? releaseDate,
        bool releaseDateIsMonthOnly,
        ReadOnlyMemory<byte> coverHash,
        string? rakutenItemUrl,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ArgumentNullException.ThrowIfNull(isbn);
        return new Volume
        {
            Id = id,
            SeriesId = seriesId,
            Isbn = isbn,
            VolumeNumber = volumeNumber,
            ReleaseDate = releaseDate,
            ReleaseDateIsMonthOnly = releaseDateIsMonthOnly,
            CoverHash = coverHash,
            RakutenItemUrl = rakutenItemUrl,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }
}
