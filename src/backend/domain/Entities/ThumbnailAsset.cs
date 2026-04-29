namespace ComiCal.Domain.Entities;

/// <summary>表紙サムネイルアセット（<c>dbo.ThumbnailAssets</c>）。<c>VolumeId</c> 1:1。</summary>
public sealed class ThumbnailAsset
{
    /// <summary>巻 ID（PK 兼 FK）。</summary>
    public Guid VolumeId { get; private set; }

    /// <summary>Blob Storage 上のキー。</summary>
    public string BlobKey { get; private set; } = string.Empty;

    /// <summary>バイトサイズ。</summary>
    public long SizeBytes { get; private set; }

    /// <summary>SHA-256 ハッシュ。</summary>
    public ReadOnlyMemory<byte> ContentHash { get; private set; }

    /// <summary>幅（px）。</summary>
    public int Width { get; private set; }

    /// <summary>高さ（px）。</summary>
    public int Height { get; private set; }

    /// <summary>論理削除フラグ。</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>論理削除日時。</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>作成日時（UTC）。</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; private set; }

    private ThumbnailAsset()
    {
    }

    /// <summary>リポジトリ層からの再構成用ファクトリ。</summary>
    public static ThumbnailAsset Hydrate(
        Guid volumeId,
        string blobKey,
        long sizeBytes,
        ReadOnlyMemory<byte> contentHash,
        int width,
        int height,
        bool isDeleted,
        DateTime? deletedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        ArgumentException.ThrowIfNullOrEmpty(blobKey);
        return new ThumbnailAsset
        {
            VolumeId = volumeId,
            BlobKey = blobKey,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            Width = width,
            Height = height,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }
}
