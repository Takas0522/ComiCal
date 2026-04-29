using ComiCal.Batch.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Orchestrators;

/// <summary>
/// Daily release-fetch orchestrator. Fan-out / fan-in across the activities:
/// StartBatchRun → FetchRakutenPage (xN) → UpsertSeriesAndVolume (xM) →
/// EnsureCoverThumbnail (xK) → FinishBatchRun. On any unhandled exception the
/// orchestrator records the failure via FailBatchRun and re-throws.
/// </summary>
/// <remarks>
/// Strict determinism: no <c>DateTime.Now</c>, no <c>Guid.NewGuid()</c>, no <c>Random</c>,
/// no I/O, no static mutable state. The <c>RunId</c> is supplied by the trigger.
/// Logging uses <c>context.CreateReplaySafeLogger</c>.
/// </remarks>
public static class DailyReleaseOrchestrator
{
    [Function(nameof(DailyReleaseOrchestrator))]
    public static async Task<BatchRunSummary> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var input = context.GetInput<BatchRunInput>()
            ?? throw new InvalidOperationException("BatchRunInput is required.");
        var logger = context.CreateReplaySafeLogger(nameof(DailyReleaseOrchestrator));

        // RetryOptions: initial 5s, max 5 attempts (initial + 4 retries), backoff 2.0.
        var retry = TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(5),
            backoffCoefficient: 2.0));

        try
        {
            // 1. Start
            await context.CallActivityAsync("StartBatchRun", input.RunId);
            logger.LogInformation(
                "DailyReleaseOrchestrator started runId={RunId} keyword={Keyword} maxPages={MaxPages}",
                input.RunId,
                input.Keyword,
                input.MaxPages);

            // 2. Fan-out fetch
            var fetchTasks = new List<Task<IReadOnlyList<BatchVolumePayload>>>(input.MaxPages);
            for (var page = 1; page <= input.MaxPages; page++)
            {
                fetchTasks.Add(context.CallActivityAsync<IReadOnlyList<BatchVolumePayload>>(
                    "FetchRakutenPage",
                    new FetchRakutenPageInput(input.Keyword, page),
                    retry));
            }

            var pages = await Task.WhenAll(fetchTasks).ConfigureAwait(false);
            var payloads = pages
                .SelectMany(p => p ?? (IReadOnlyList<BatchVolumePayload>)Array.Empty<BatchVolumePayload>())
                .ToList();

            logger.LogInformation(
                "DailyReleaseOrchestrator fetched {IsbnCount} payloads across {Pages} pages",
                payloads.Count,
                input.MaxPages);

            // 3. Fan-out upsert
            var upsertTasks = payloads
                .Select(p => context.CallActivityAsync<UpsertResult>("UpsertSeriesAndVolume", p, retry))
                .ToList();
            var upserts = await Task.WhenAll(upsertTasks).ConfigureAwait(false);

            // 4. Fan-out cover download for items with a cover URL
            var coverTasks = upserts
                .Where(u => !string.IsNullOrWhiteSpace(u.CoverUrl))
                .Select(u => context.CallActivityAsync(
                    "EnsureCoverThumbnail",
                    new CoverDownloadInput(u.Isbn, u.VolumeId, u.CoverUrl!, u.CurrentCoverHash),
                    retry))
                .ToList();
            await Task.WhenAll(coverTasks).ConfigureAwait(false);

            // 5. Finish
            var summary = new BatchRunSummary(
                FetchedItems: payloads.Count,
                UpsertedVolumes: upserts.Count(u => u.IsNew),
                FailedItems: 0);

            await context.CallActivityAsync("FinishBatchRun", new FinishBatchRunInput(input.RunId, summary));
            logger.LogInformation(
                "DailyReleaseOrchestrator completed runId={RunId} status={Status} fetched={IsbnCount} upserted={UpsertedCount}",
                input.RunId,
                "Succeeded",
                summary.FetchedItems,
                summary.UpsertedVolumes);
            return summary;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failInput = new FailBatchRunInput(
                input.RunId,
                ex.Message,
                new[] { new FailedItemRecord("orchestrator", ex.GetType().FullName ?? "Exception", null) });
            await context.CallActivityAsync("FailBatchRun", failInput);
            throw;
        }
    }
}
