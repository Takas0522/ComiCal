namespace ComiCal.Application.Dtos;
public record VolumeDto(
    Guid VolumeId,
    string Isbn13,
    int? VolumeNumber,
    DateOnly? ReleaseDate,
    bool ReleaseDateIsMonthOnly,
    string? ThumbnailUrl,
    string? RakutenItemUrl);
