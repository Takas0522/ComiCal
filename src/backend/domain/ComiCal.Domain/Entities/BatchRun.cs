using ComiCal.Domain.Enums;

namespace ComiCal.Domain.Entities;

public sealed class BatchRun
{
    public Guid BatchRunId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public BatchRunStatus Status { get; private set; }
    public int FetchedItemCount { get; private set; }
    public int UpsertedVolumeCount { get; private set; }
    public int DownloadedThumbnailCount { get; private set; }
    public int SkippedThumbnailCount { get; private set; }
    public int FailedItemCount { get; private set; }

    private readonly List<FailedItem> _failedItems = [];
    public IReadOnlyCollection<FailedItem> FailedItems => _failedItems.AsReadOnly();

    private BatchRun(Guid batchRunId, DateTime startedAt)
    {
        BatchRunId = batchRunId;
        StartedAt = startedAt;
        Status = BatchRunStatus.Running;
    }

    public static BatchRun Create()
        => new(Guid.NewGuid(), DateTime.UtcNow);

    public void Complete(int fetchedItemCount, int upsertedVolumeCount, int downloadedThumbnailCount, int skippedThumbnailCount, int failedItemCount)
    {
        Status = failedItemCount > 0 ? BatchRunStatus.Failed : BatchRunStatus.Succeeded;
        FetchedItemCount = fetchedItemCount;
        UpsertedVolumeCount = upsertedVolumeCount;
        DownloadedThumbnailCount = downloadedThumbnailCount;
        SkippedThumbnailCount = skippedThumbnailCount;
        FailedItemCount = failedItemCount;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = BatchRunStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void AddFailedItem(FailedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _failedItems.Add(item);
    }
}
