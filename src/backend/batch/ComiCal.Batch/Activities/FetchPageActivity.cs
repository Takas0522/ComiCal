using ComiCal.Batch.Models;
using ComiCal.Infrastructure.Rakuten;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

public partial class FetchPageActivity(
    RakutenBooksApiClient rakutenClient,
    ILogger<FetchPageActivity> logger)
{
    [Function("FetchPageActivity")]
    public async Task<FetchPageOutput> Run([ActivityTrigger] FetchPageInput input)
    {
        var result = await rakutenClient.SearchComicsAsync(
            input.Page, input.ReleaseDateFrom, input.ReleaseDateTo);

        var items = result.Items.Select(item => new RakutenVolumeData(
            item.Isbn,
            item.Title,
            item.Author,
            item.PublisherName,
            item.SalesDate,
            string.IsNullOrWhiteSpace(item.LargeImageUrl) ? null : item.LargeImageUrl,
            string.IsNullOrWhiteSpace(item.ItemUrl) ? null : item.ItemUrl
        )).ToList();

        // Rakuten Books Search API caps queryable pages at 100 (page 101+ returns HTTP 400).
        // The `Last` field in the response can exceed this cap, so we clamp here to
        // prevent FetchOrchestrator from chaining ContinueAsNew past the limit.
        const int RakutenMaxPage = 100;
        var totalPages = Math.Min(result.Last, RakutenMaxPage);

        LogPageFetched(logger, input.Page, totalPages, items.Count);

        return new FetchPageOutput(totalPages, items.Count, items);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched page {Page}/{TotalPages}: {Count} items")]
    private static partial void LogPageFetched(ILogger logger, int page, int totalPages, int count);
}
