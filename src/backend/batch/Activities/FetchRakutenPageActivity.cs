using ComiCal.Batch.Models;
using ComiCal.Batch.Observability;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Rakuten.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Calls the Rakuten Books search API (genre 001001 / コミック) for a single page
/// and maps each item via <see cref="RakutenItemMapper.TryMap"/> to a serializable
/// <see cref="BatchVolumePayload"/>. Items that fail ISBN-13 validation are dropped.
/// </summary>
public sealed class FetchRakutenPageActivity(
    IRakutenBooksClient client,
    IBatchMetrics metrics,
    ILogger<FetchRakutenPageActivity> logger)
{
    private const string RakutenSearchEndpoint = "BooksBook/Search";

    private readonly IRakutenBooksClient _client = client;
    private readonly IBatchMetrics _metrics = metrics;
    private readonly ILogger<FetchRakutenPageActivity> _logger = logger;

    [Function("FetchRakutenPage")]
    public async Task<IReadOnlyList<BatchVolumePayload>> RunAsync(
        [ActivityTrigger] FetchRakutenPageInput input,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        _metrics.RecordRakutenCall(RakutenSearchEndpoint);

        RakutenSearchResponse response;
        try
        {
            response = await _client
                .SearchByGenreAsync(input.Keyword, input.Page, ct)
                .ConfigureAwait(false);
        }
        catch (RakutenBooksApiException ex) when (ex.StatusCode == 429)
        {
            _metrics.RecordRakutenRateLimited(RakutenSearchEndpoint);
            throw;
        }

        var result = new List<BatchVolumePayload>(response.Items.Count);
        foreach (var envelope in response.Items)
        {
            var payload = RakutenItemMapper.TryMap(envelope.Item);
            if (payload is null)
            {
                continue;
            }

            result.Add(new BatchVolumePayload(
                Isbn: payload.Isbn.Value,
                Title: payload.Title,
                SeriesName: payload.SeriesName,
                SeriesNameKana: payload.SeriesNameKana,
                VolumeNumber: payload.VolumeNumber,
                ReleaseDate: payload.ReleaseDate,
                ReleaseDateIsMonthOnly: payload.ReleaseDateIsMonthOnly,
                AuthorName: payload.AuthorName,
                AuthorKana: payload.AuthorKana,
                PublisherName: payload.PublisherName,
                ItemUrl: payload.ItemUrl,
                CoverImageUrl: payload.CoverImageUrl));
        }

        _logger.LogInformation(
            "FetchRakutenPage page={Page} keyword={Keyword} returned {ValidCount} valid items (raw {RawCount})",
            input.Page,
            input.Keyword,
            result.Count,
            response.Items.Count);

        return result;
    }
}
