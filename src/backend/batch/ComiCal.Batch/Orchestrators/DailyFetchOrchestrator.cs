using ComiCal.Batch.Models;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Orchestrators;

[DurableTask("DailyFetchOrchestrator")]
public class DailyFetchOrchestrator : TaskOrchestrator<object?, string>
{
    private static readonly Action<ILogger, Guid, Exception?> LogBatchRunCreated =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(1, "BatchRunCreated"), "BatchRun created: {BatchRunId}");

    public override async Task<string> RunAsync(TaskOrchestrationContext context, object? _)
    {
        var logger = context.CreateReplaySafeLogger<DailyFetchOrchestrator>();
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(3, TimeSpan.FromSeconds(5), 2.0));

        // 1. Create BatchRun record
        var batchRunId = await context.CallActivityAsync<Guid>(
            "CreateBatchRunActivity", null, retryOptions);
        LogBatchRunCreated(logger, batchRunId, null);

        // Date range: 6 months back to 6 months ahead
        var today = context.CurrentUtcDateTime;
        var releaseDateFrom = DateOnly.FromDateTime(today.AddMonths(-6));
        var releaseDateTo = DateOnly.FromDateTime(today.AddMonths(6));

        // 2. Fetch all pages via sub-orchestrator (chaining with ContinueAsNew)
        var fetchResult = await context.CallSubOrchestratorAsync<FetchSummary>(
            "FetchOrchestrator",
            new FetchInput(batchRunId, 1, releaseDateFrom, releaseDateTo, 0, 0, []));

        // 3. Thumbnail fan-out/fan-in
        var (downloadedCount, skippedCount, thumbFailedCount) = (0, 0, 0);
        if (fetchResult.ThumbnailPending.Count > 0)
        {
            var thumbResult = await context.CallSubOrchestratorAsync<ThumbnailSummary>(
                "ThumbnailOrchestrator",
                new ThumbnailInput(batchRunId, fetchResult.ThumbnailPending));
            downloadedCount = thumbResult.DownloadedCount;
            skippedCount = thumbResult.SkippedCount;
            thumbFailedCount = thumbResult.FailedCount;
        }

        // 4. Finalize
        await context.CallActivityAsync<bool>(
            "FinalizeBatchRunActivity",
            new FinalizeBatchRunInput(
                batchRunId,
                fetchResult.FetchedCount,
                fetchResult.UpsertedCount,
                downloadedCount,
                skippedCount,
                fetchResult.FailedCount + thumbFailedCount,
                true),
            retryOptions);

        return batchRunId.ToString();
    }
}

public record FetchInput(Guid BatchRunId, int Page, DateOnly ReleaseDateFrom, DateOnly ReleaseDateTo, int AccumFetched, int AccumUpserted, IReadOnlyList<ThumbnailPendingItem> AccumThumbnails);
public record FetchSummary(int FetchedCount, int UpsertedCount, int FailedCount, IReadOnlyList<ThumbnailPendingItem> ThumbnailPending);
public record ThumbnailInput(Guid BatchRunId, IReadOnlyList<ThumbnailPendingItem> Items);
public record ThumbnailSummary(int DownloadedCount, int SkippedCount, int FailedCount);
