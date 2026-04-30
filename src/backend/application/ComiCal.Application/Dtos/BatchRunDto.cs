namespace ComiCal.Application.Dtos;
public record BatchRunDto(
    Guid BatchRunId,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    int FetchedItemCount,
    int UpsertedVolumeCount,
    int DownloadedThumbnailCount,
    int SkippedThumbnailCount,
    int FailedItemCount);
