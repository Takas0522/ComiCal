namespace ComiCal.Batch.Models;

public record FetchPageInput(Guid BatchRunId, int Page, DateOnly? ReleaseDateFrom, DateOnly? ReleaseDateTo);
public record FetchPageOutput(int TotalPages, int FetchedCount, IReadOnlyList<RakutenVolumeData> Items);
public record RakutenVolumeData(string Isbn13, string RawTitle, string AuthorRaw, string PublisherName, string SalesDate, string? LargeImageUrl, string? ItemUrl);
public record UpsertVolumesInput(Guid BatchRunId, IReadOnlyList<RakutenVolumeData> Items);
public record UpsertVolumesOutput(int UpsertedCount, IReadOnlyList<ThumbnailPendingItem> ThumbnailPending, IReadOnlyList<string> FailedIsbn13s);
public record ThumbnailPendingItem(Guid VolumeId, string ImageUrl, byte[]? ExistingHash);
public record DownloadThumbnailInput(Guid BatchRunId, Guid VolumeId, string ImageUrl, byte[]? ExistingHash);
public record DownloadThumbnailOutput(bool Downloaded, bool Skipped, bool Failed, string? FailureReason);
public record FinalizeBatchRunInput(Guid BatchRunId, int FetchedItemCount, int UpsertedVolumeCount, int DownloadedCount, int SkippedCount, int FailedCount, bool Success);
