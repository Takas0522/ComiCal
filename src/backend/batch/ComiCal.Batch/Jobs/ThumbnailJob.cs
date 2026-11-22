using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Jobs;

public sealed partial class ThumbnailJob(IActivityInvoker invoker, ILogger<ThumbnailJob> logger)
{
    public async Task<ThumbnailSummary> RunAsync(
        Guid batchRunId, IReadOnlyList<ThumbnailPendingItem> items, CancellationToken ct)
    {
        var downloaded = 0;
        var skipped = 0;
        var failed = 0;

        await Parallel.ForEachAsync(items, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
            async (item, token) =>
            {
                DownloadThumbnailOutput result;
                try
                {
                    result = await invoker.RunAsync<DownloadThumbnailActivity, DownloadThumbnailInput, DownloadThumbnailOutput>(
                        new(batchRunId, item.VolumeId, item.ImageUrl, item.ExistingHash),
                        static (activity, input, attemptToken) => activity.Run(input, attemptToken), token, thumbnail: true);
                }
                catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
                {
                    // リトライを使い切った HttpClient のタイムアウトは、その 1 件の失敗として扱い、残りの処理を続ける。
                    LogThumbnailTimedOut(logger, item.VolumeId, ex);
                    result = new DownloadThumbnailOutput(false, false, true, "timeout");
                }

                if (result.Downloaded) Interlocked.Increment(ref downloaded);
                if (result.Skipped) Interlocked.Increment(ref skipped);
                if (result.Failed) Interlocked.Increment(ref failed);
            });

        LogThumbnailsDone(logger, downloaded, skipped, failed);
        return new ThumbnailSummary(downloaded, skipped, failed);
    }

    [LoggerMessage(EventId = 2, EventName = "ThumbnailTimedOut", Level = LogLevel.Warning, Message = "Thumbnail for volume {VolumeId} timed out after retries")]
    private static partial void LogThumbnailTimedOut(ILogger logger, Guid volumeId, Exception exception);

    [LoggerMessage(EventId = 1, EventName = "ThumbnailsDone", Level = LogLevel.Information, Message = "Thumbnails: downloaded={Downloaded}, skipped={Skipped}, failed={Failed}")]
    private static partial void LogThumbnailsDone(ILogger logger, int downloaded, int skipped, int failed);
}

public record ThumbnailSummary(int DownloadedCount, int SkippedCount, int FailedCount);
