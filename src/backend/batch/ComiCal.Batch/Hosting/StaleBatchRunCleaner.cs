using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Hosting;

public interface IStaleBatchRunCleaner
{
    Task CleanAsync(DateTime startedBeforeUtc, CancellationToken ct);
}

public sealed partial class StaleBatchRunCleaner(
    IBatchRunRepository runs, ILogger<StaleBatchRunCleaner> logger) : IStaleBatchRunCleaner
{
    public async Task CleanAsync(DateTime startedBeforeUtc, CancellationToken ct)
    {
        foreach (var run in await runs.FindStaleAsync(startedBeforeUtc, ct))
        {
            run.Complete(run.FetchedItemCount, run.UpsertedVolumeCount, run.DownloadedThumbnailCount,
                run.SkippedThumbnailCount, Math.Max(1, run.FailedItemCount));
            await runs.UpdateAsync(run, ct);
            LogStaleRun(logger, run.BatchRunId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Marked stale batch run {BatchRunId} as failed.")]
    private static partial void LogStaleRun(ILogger logger, Guid batchRunId);
}
