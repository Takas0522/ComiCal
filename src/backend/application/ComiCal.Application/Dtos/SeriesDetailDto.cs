namespace ComiCal.Application.Dtos;
public record SeriesDetailDto(
    Guid SeriesId,
    string Title,
    bool IsCompleted,
    PublisherDto? Publisher,
    IReadOnlyList<AuthorDto> Authors,
    IReadOnlyList<VolumeDto> Volumes);
