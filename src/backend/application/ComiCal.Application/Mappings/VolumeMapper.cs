using ComiCal.Application.Dtos;
using ComiCal.Domain.Entities;

namespace ComiCal.Application.Mappings;

public static class VolumeMapper
{
    public static VolumeDto ToDto(Volume volume, string? blobBaseUrl = null)
    {
        string? thumbnailUrl = null;
        if (volume.ThumbnailAsset is not null && blobBaseUrl is not null)
        {
            thumbnailUrl = $"{blobBaseUrl.TrimEnd('/')}/{volume.ThumbnailAsset.BlobKey}";
        }

        DateOnly? releaseDate = volume.ReleaseDate.HasValue
            ? DateOnly.FromDateTime(volume.ReleaseDate.Value)
            : null;

        VolumeSeriesRef? seriesRef = volume.Series is null
            ? null
            : new VolumeSeriesRef(volume.Series.SeriesId, volume.Series.Title);

        return new VolumeDto(
            volume.VolumeId,
            volume.Isbn13,
            volume.VolumeNumber,
            releaseDate,
            volume.ReleaseDateIsMonthOnly,
            thumbnailUrl,
            volume.RakutenItemUrl,
            seriesRef);
    }
}
