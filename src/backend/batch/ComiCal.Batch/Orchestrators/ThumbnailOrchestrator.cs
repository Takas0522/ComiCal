using ComiCal.Batch.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Orchestrators;

public static class ThumbnailOrchestrator
{
    private const int MaxParallelism = 8;

    private static readonly Action<ILogger, int, int, int, Exception?> LogThumbnailsDone =
        LoggerMessage.Define<int, int, int>(LogLevel.Information, new EventId(1, "ThumbnailsDone"),
            "Thumbnails: downloaded={Downloaded}, skipped={Skipped}, failed={Failed}");

    [Function("ThumbnailOrchestrator")]
    public static async Task<ThumbnailSummary> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ThumbnailInput>()!;
        var logger = context.CreateReplaySafeLogger("ThumbnailOrchestrator");
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(3, TimeSpan.FromSeconds(3), 2.0));

        var downloaded = 0;
        var skipped = 0;
        var failed = 0;

        // Process in batches of MaxParallelism
        for (int i = 0; i < input.Items.Count; i += MaxParallelism)
        {
            var batch = input.Items.Skip(i).Take(MaxParallelism);
            var tasks = batch.Select(item =>
                context.CallActivityAsync<DownloadThumbnailOutput>(
                    "DownloadThumbnailActivity",
                    new DownloadThumbnailInput(input.BatchRunId, item.VolumeId, item.ImageUrl, item.ExistingHash),
                    retryOptions));

            var results = await Task.WhenAll(tasks);
            downloaded += results.Count(r => r.Downloaded);
            skipped += results.Count(r => r.Skipped);
            failed += results.Count(r => r.Failed);
        }

        LogThumbnailsDone(logger, downloaded, skipped, failed, null);

        return new ThumbnailSummary(downloaded, skipped, failed);
    }
}
