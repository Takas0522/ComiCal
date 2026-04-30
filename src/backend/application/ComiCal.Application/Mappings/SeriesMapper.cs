using ComiCal.Application.Dtos;
using ComiCal.Domain.Entities;

namespace ComiCal.Application.Mappings;

public static class SeriesMapper
{
    public static SeriesDto ToDto(Series series, string? blobBaseUrl = null)
    {
        var authors = series.SeriesAuthors
            .Select(sa => new AuthorDto(
                sa.AuthorId,
                sa.Author?.Name ?? string.Empty,
                sa.Role.ToString()))
            .ToList();

        var now = DateTime.UtcNow.Date;
        var released = series.Volumes
            .Where(v => !v.IsDeleted && v.ReleaseDate.HasValue && v.ReleaseDate.Value.Date <= now)
            .OrderByDescending(v => v.ReleaseDate)
            .FirstOrDefault();
        var next = series.Volumes
            .Where(v => !v.IsDeleted && v.ReleaseDate.HasValue && v.ReleaseDate.Value.Date > now)
            .OrderBy(v => v.ReleaseDate)
            .FirstOrDefault();

        return new SeriesDto(
            series.SeriesId,
            series.Title,
            series.IsCompleted,
            series.Publisher is null ? null : new PublisherDto(series.Publisher.PublisherId, series.Publisher.Name),
            authors,
            released is null ? null : VolumeMapper.ToDto(released, blobBaseUrl),
            next is null ? null : VolumeMapper.ToDto(next, blobBaseUrl));
    }

    public static SeriesDetailDto ToDetailDto(Series series, string? blobBaseUrl = null)
    {
        var authors = series.SeriesAuthors
            .Select(sa => new AuthorDto(
                sa.AuthorId,
                sa.Author?.Name ?? string.Empty,
                sa.Role.ToString()))
            .ToList();

        var volumes = series.Volumes
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.VolumeNumber)
            .ThenBy(v => v.ReleaseDate)
            .Select(v => VolumeMapper.ToDto(v, blobBaseUrl))
            .ToList();

        return new SeriesDetailDto(
            series.SeriesId,
            series.Title,
            series.IsCompleted,
            series.Publisher is null ? null : new PublisherDto(series.Publisher.PublisherId, series.Publisher.Name),
            authors,
            volumes);
    }
}
