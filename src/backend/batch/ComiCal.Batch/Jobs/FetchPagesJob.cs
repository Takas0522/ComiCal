using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Jobs;

public sealed partial class FetchPagesJob(IActivityInvoker invoker, ILogger<FetchPagesJob> logger)
{
    public async Task<FetchSummary> RunAsync(
        Guid batchRunId, DateOnly releaseDateFrom, DateOnly releaseDateTo, CancellationToken ct)
    {
        var fetched = 0;
        var upserted = 0;
        var failed = 0;
        var thumbnailPending = new List<ThumbnailPendingItem>();
        var totalPages = 1;

        for (var page = 1; page <= totalPages; page++)
        {
            var fetch = await invoker.RunAsync<FetchPageActivity, FetchPageInput, FetchPageOutput>(
                new(batchRunId, page, releaseDateFrom, releaseDateTo),
                static (activity, input, token) => activity.Run(input, token), ct);
            var upsert = await invoker.RunAsync<UpsertVolumesActivity, UpsertVolumesInput, UpsertVolumesOutput>(
                new(batchRunId, fetch.Items),
                static (activity, input, token) => activity.Run(input, token), ct);

            fetched += fetch.FetchedCount;
            upserted += upsert.UpsertedCount;
            // 旧 FetchOrchestrator は最終ページの件数で上書きしていた（ContinueAsNew で持ち回していなかった）。全ページ分を合計する。
            failed += upsert.FailedIsbn13s.Count;
            thumbnailPending.AddRange(upsert.ThumbnailPending);
            totalPages = fetch.TotalPages;
            LogPageFetched(logger, page, totalPages, fetch.FetchedCount);
        }

        return new FetchSummary(fetched, upserted, failed, thumbnailPending);
    }

    [LoggerMessage(EventId = 1, EventName = "PageFetched", Level = LogLevel.Information, Message = "Page {Page}/{TotalPages} fetched: {FetchedCount} items")]
    private static partial void LogPageFetched(ILogger logger, int page, int totalPages, int fetchedCount);
}

public record FetchSummary(int FetchedCount, int UpsertedCount, int FailedCount, IReadOnlyList<ThumbnailPendingItem> ThumbnailPending);
