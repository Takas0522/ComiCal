using ComiCal.Application.DTOs;
using ComiCal.Domain.Entities;

namespace ComiCal.Application.Mappings;

/// <summary>Domain Entity → DTO 変換の拡張メソッド群。</summary>
public static class DomainMappings
{
    /// <summary>巻を <see cref="VolumeDto"/> に変換する（Thumbnail は呼び出し側で注入）。</summary>
    public static VolumeDto ToDto(this Volume volume, ThumbnailAsset? thumbnail = null)
    {
        ArgumentNullException.ThrowIfNull(volume);
        return new VolumeDto(
            Id: volume.Id,
            SeriesId: volume.SeriesId,
            Isbn: volume.Isbn.Value,
            VolumeNumber: volume.VolumeNumber,
            ReleaseDate: volume.ReleaseDate,
            ReleaseDateIsMonthOnly: volume.ReleaseDateIsMonthOnly,
            RakutenItemUrl: volume.RakutenItemUrl,
            Thumbnail: thumbnail?.ToDto());
    }

    /// <summary>サムネイルアセットを DTO に変換する。</summary>
    public static ThumbnailDto ToDto(this ThumbnailAsset thumbnail)
    {
        ArgumentNullException.ThrowIfNull(thumbnail);
        return new ThumbnailDto(thumbnail.BlobKey, thumbnail.Width, thumbnail.Height);
    }

    /// <summary>シリーズを概要 DTO に変換する。</summary>
    public static SeriesSummaryDto ToSummaryDto(this Series series)
    {
        ArgumentNullException.ThrowIfNull(series);
        return new SeriesSummaryDto(
            Id: series.Id,
            Title: series.Title,
            PublisherId: series.PublisherId,
            PrimaryAuthorId: series.PrimaryAuthorId,
            IsCompleted: series.IsCompleted);
    }

    /// <summary>著者を DTO に変換する。</summary>
    public static AuthorDto ToDto(this Author author)
    {
        ArgumentNullException.ThrowIfNull(author);
        return new AuthorDto(author.Id, author.Name);
    }

    /// <summary>出版社を DTO に変換する。</summary>
    public static PublisherDto ToDto(this Publisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        return new PublisherDto(publisher.Id, publisher.Name);
    }
}
