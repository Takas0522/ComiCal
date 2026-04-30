using ComiCal.Batch.Models;
using ComiCal.Infrastructure.Rakuten;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

[DurableTask("FetchPageActivity")]
public partial class FetchPageActivity(
    RakutenBooksApiClient rakutenClient,
    ILogger<FetchPageActivity> logger)
    : TaskActivity<FetchPageInput, FetchPageOutput>
{
    public override async Task<FetchPageOutput> RunAsync(TaskActivityContext context, FetchPageInput input)
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

        LogPageFetched(logger, input.Page, result.Last, items.Count);

        return new FetchPageOutput(result.Last, items.Count, items);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched page {Page}/{TotalPages}: {Count} items")]
    private static partial void LogPageFetched(ILogger logger, int page, int totalPages, int count);
}
