using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Jobs;

public sealed partial class DailyFetchJob(
    IActivityInvoker invoker, FetchPagesJob fetch, ThumbnailJob thumbnails, TimeProvider clock, ILogger<DailyFetchJob> logger)
{
    public async Task<Guid> RunAsync(CancellationToken ct)
    {
        var batchRunId = await invoker.RunAsync<CreateBatchRunActivity, object?, Guid>(
            null, static (activity, input, token) => activity.Run(input, token), ct);

        try
        {
            LogBatchRunCreated(logger, batchRunId);
            var today = clock.GetUtcNow().UtcDateTime;
            var fetched = await fetch.RunAsync(batchRunId, DateOnly.FromDateTime(today.AddMonths(-6)),
                DateOnly.FromDateTime(today.AddMonths(6)), ct);
            var thumbs = fetched.ThumbnailPending.Count > 0
                ? await thumbnails.RunAsync(batchRunId, fetched.ThumbnailPending, ct)
                : new ThumbnailSummary(0, 0, 0);

            await FinalizeAsync(new(batchRunId, fetched.FetchedCount, fetched.UpsertedCount,
                thumbs.DownloadedCount, thumbs.SkippedCount, fetched.FailedCount + thumbs.FailedCount, true), ct);
            return batchRunId;
        }
        catch
        {
            await FinalizeAsync(new(batchRunId, 0, 0, 0, 0, 1, false), CancellationToken.None);
            throw;
        }
    }

    private Task<bool> FinalizeAsync(FinalizeBatchRunInput input, CancellationToken ct)
        => invoker.RunAsync<FinalizeBatchRunActivity, FinalizeBatchRunInput, bool>(
            input, static (activity, request, token) => activity.Run(request, token), ct);

    [LoggerMessage(EventId = 1, EventName = "BatchRunCreated", Level = LogLevel.Information, Message = "BatchRun created: {BatchRunId}")]
    private static partial void LogBatchRunCreated(ILogger logger, Guid batchRunId);
}
