namespace ComiCal.Domain.Entities;

public sealed class ThumbnailAsset
{
    public Guid VolumeId { get; private set; }
    public string BlobKey { get; private set; }
    public long SizeBytes { get; private set; }
    public byte[] ContentHash { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ThumbnailAsset(Guid volumeId, string blobKey, long sizeBytes, byte[] contentHash, int width, int height, DateTime createdAt)
    {
        VolumeId = volumeId;
        BlobKey = blobKey;
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        Width = width;
        Height = height;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static ThumbnailAsset Create(Guid volumeId, string blobKey, long sizeBytes, byte[] contentHash, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        ArgumentNullException.ThrowIfNull(contentHash);
        var now = DateTime.UtcNow;
        return new ThumbnailAsset(volumeId, blobKey[..Math.Min(blobKey.Length, 256)], sizeBytes, contentHash, width, height, now);
    }

    public void Update(string blobKey, long sizeBytes, byte[] contentHash, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);
        ArgumentNullException.ThrowIfNull(contentHash);
        BlobKey = blobKey[..Math.Min(blobKey.Length, 256)];
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        Width = width;
        Height = height;
        UpdatedAt = DateTime.UtcNow;
    }
}
