namespace ComiCal.Application.Dtos;
public record SeriesDto(
    Guid SeriesId,
    string Title,
    bool IsCompleted,
    PublisherDto? Publisher,
    IReadOnlyList<AuthorDto> Authors,
    VolumeDto? LatestVolume,
    VolumeDto? NextVolume);
