using ComiCal.Batch.Models;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

[DurableTask("UpsertVolumesActivity")]
public partial class UpsertVolumesActivity(
    IVolumeRepository volumeRepo,
    IBatchRunRepository batchRunRepo,
    ILogger<UpsertVolumesActivity> logger)
    : TaskActivity<UpsertVolumesInput, UpsertVolumesOutput>
{
    public override async Task<UpsertVolumesOutput> RunAsync(TaskActivityContext context, UpsertVolumesInput input)
    {
        var upserted = 0;
        var thumbnailPending = new List<ThumbnailPendingItem>();
        var failedIsbns = new List<string>();

        foreach (var item in input.Items)
        {
            try
            {
                if (item.Isbn13.Length != 13 || !item.Isbn13.All(char.IsDigit))
                {
                    LogInvalidIsbn(logger, item.Isbn13);
                    failedIsbns.Add(item.Isbn13);
                    continue;
                }

                DateTime? releaseDate = null;
                var isMonthOnly = false;
                if (!string.IsNullOrWhiteSpace(item.SalesDate))
                {
                    if (DateTime.TryParseExact(item.SalesDate, "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out var exactDate))
                    {
                        releaseDate = exactDate;
                    }
                    else if (DateTime.TryParseExact(item.SalesDate, "yyyy-MM", null,
                        System.Globalization.DateTimeStyles.None, out var monthDate))
                    {
                        releaseDate = new DateTime(monthDate.Year, monthDate.Month,
                            DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
                        isMonthOnly = true;
                    }
                }

                var existing = await volumeRepo.FindByIsbnAsync(item.Isbn13);

                if (existing is not null)
                {
                    if (item.LargeImageUrl is not null)
                        thumbnailPending.Add(new ThumbnailPendingItem(existing.VolumeId, item.LargeImageUrl, existing.CoverHash));

                    existing.UpdateRakutenItemUrl(item.ItemUrl);
                    await volumeRepo.UpsertAsync(existing);
                }
                else
                {
                    // Simplified series lookup: derive normalized title and use as series key.
                    // Full author/series resolution would require an IAuthorRepository (not yet available).
                    var volumeNumber = VolumeNumberExtractor.Extract(item.RawTitle);
                    var seriesId = Guid.NewGuid();
                    var volume = Volume.Create(seriesId, item.Isbn13, volumeNumber, releaseDate, isMonthOnly);
                    volume.UpdateRakutenItemUrl(item.ItemUrl);
                    await volumeRepo.UpsertAsync(volume);

                    if (item.LargeImageUrl is not null)
                        thumbnailPending.Add(new ThumbnailPendingItem(volume.VolumeId, item.LargeImageUrl, null));
                }

                upserted++;
            }
            catch (Exception ex)
            {
                LogUpsertFailed(logger, item.Isbn13, ex);
                failedIsbns.Add(item.Isbn13);
                await batchRunRepo.AddFailedItemAsync(
                    FailedItem.Create(input.BatchRunId, item.Isbn13, ex.Message));
            }
        }

        return new UpsertVolumesOutput(upserted, thumbnailPending, failedIsbns);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid ISBN: {Isbn}")]
    private static partial void LogInvalidIsbn(ILogger logger, string isbn);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to upsert volume {Isbn}")]
    private static partial void LogUpsertFailed(ILogger logger, string isbn, Exception ex);
}
