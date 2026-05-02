namespace ComiCal.Domain.Entities;

public sealed class Volume
{
    public Guid VolumeId { get; private set; }
    public Guid SeriesId { get; private set; }
    public string Isbn13 { get; private set; }
    public int? VolumeNumber { get; private set; }
    public DateTime? ReleaseDate { get; private set; }
    public bool ReleaseDateIsMonthOnly { get; private set; }
    public byte[]? CoverHash { get; private set; }
    public string? RakutenItemUrl { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ThumbnailAsset? ThumbnailAsset { get; private set; }
    public Series? Series { get; private set; }

    private Volume(Guid volumeId, Guid seriesId, string isbn13, DateTime createdAt)
    {
        VolumeId = volumeId;
        SeriesId = seriesId;
        Isbn13 = isbn13;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Volume Create(Guid seriesId, string isbn13, int? volumeNumber = null, DateTime? releaseDate = null, bool releaseDateIsMonthOnly = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn13);
        var now = DateTime.UtcNow;
        return new Volume(Guid.NewGuid(), seriesId, isbn13, now)
        {
            VolumeNumber = volumeNumber,
            ReleaseDate = releaseDate,
            ReleaseDateIsMonthOnly = releaseDateIsMonthOnly
        };
    }

    public void UpdateCoverHash(byte[]? coverHash)
    {
        CoverHash = coverHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRakutenItemUrl(string? url)
    {
        RakutenItemUrl = url is null ? null : url[..Math.Min(url.Length, 512)];
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
