using ComiCal.Batch.Models;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Orchestrators;

[DurableTask("FetchOrchestrator")]
public class FetchOrchestrator : TaskOrchestrator<FetchInput, FetchSummary>
{
    private static readonly Action<ILogger, int, int, int, Exception?> LogPageFetched =
        LoggerMessage.Define<int, int, int>(LogLevel.Information, new EventId(1, "PageFetched"),
            "Page {Page}/{TotalPages} fetched: {FetchedCount} items");

    public override async Task<FetchSummary> RunAsync(TaskOrchestrationContext context, FetchInput input)
    {
        var logger = context.CreateReplaySafeLogger<FetchOrchestrator>();
        var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(3, TimeSpan.FromSeconds(5), 2.0));

        // Fetch this page
        var fetchOutput = await context.CallActivityAsync<FetchPageOutput>(
            "FetchPageActivity",
            new FetchPageInput(input.BatchRunId, input.Page, input.ReleaseDateFrom, input.ReleaseDateTo),
            retryOptions);

        // Upsert volumes
        var upsertOutput = await context.CallActivityAsync<UpsertVolumesOutput>(
            "UpsertVolumesActivity",
            new UpsertVolumesInput(input.BatchRunId, fetchOutput.Items),
            retryOptions);

        var totalFetched = input.AccumFetched + fetchOutput.FetchedCount;
        var totalUpserted = input.AccumUpserted + upsertOutput.UpsertedCount;
        var allThumbnails = input.AccumThumbnails.Concat(upsertOutput.ThumbnailPending).ToList();
        var totalFailed = upsertOutput.FailedIsbn13s.Count;

        LogPageFetched(logger, input.Page, fetchOutput.TotalPages, fetchOutput.FetchedCount, null);

        // ContinueAsNew for next page to avoid history bloat
        if (input.Page < fetchOutput.TotalPages)
        {
            context.ContinueAsNew(input with
            {
                Page = input.Page + 1,
                AccumFetched = totalFetched,
                AccumUpserted = totalUpserted,
                AccumThumbnails = allThumbnails
            }, false);
            return null!; // Unreachable after ContinueAsNew
        }

        return new FetchSummary(totalFetched, totalUpserted, totalFailed, allThumbnails);
    }
}
