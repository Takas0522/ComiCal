using ComiCal.Batch.Models;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

public partial class UpsertVolumesActivity(
    IVolumeRepository volumeRepo,
    ISeriesRepository seriesRepo,
    IAuthorRepository authorRepo,
    IPublisherRepository publisherRepo,
    IBatchRunRepository batchRunRepo,
    ILogger<UpsertVolumesActivity> logger)
{
    [Function("UpsertVolumesActivity")]
    public async Task<UpsertVolumesOutput> Run([ActivityTrigger] UpsertVolumesInput input)
    {
        var upserted = 0;
        var thumbnailPending = new List<ThumbnailPendingItem>();
        var failedIsbns = new List<string>();
        var seriesCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var authorCache = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var publisherCache = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var item in input.Items)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.Isbn13)
                    || item.Isbn13.Length != 13
                    || !item.Isbn13.All(char.IsDigit))
                {
                    LogInvalidIsbn(logger, item.Isbn13 ?? "(null)");
                    if (!string.IsNullOrWhiteSpace(item.Isbn13))
                        failedIsbns.Add(item.Isbn13);
                    continue;
                }

                var (releaseDate, isMonthOnly) = ParseSalesDate(item.SalesDate);

                // Resolve authors (split raw "A／B/C、D" → individual normalized names → upsert)
                var authorIds = await ResolveAuthorIdsAsync(item.AuthorRaw, authorCache);
                var primaryAuthorId = authorIds.Count > 0 ? authorIds[0] : (Guid?)null;

                // Resolve publisher
                Guid? publisherId = null;
                if (!string.IsNullOrWhiteSpace(item.PublisherName))
                {
                    publisherId = await ResolvePublisherIdAsync(item.PublisherName, publisherCache);
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
                    var volumeNumber = VolumeNumberExtractor.Extract(item.RawTitle);
                    var normalizedTitle = SeriesAggregator.ComputeNormalizedTitle(item.RawTitle);

                    // SeriesId: deterministic by (normalizedTitle + primaryAuthorId)
                    // so the same series is re-used across pages and batches.
                    var seriesKey = $"{normalizedTitle}|{primaryAuthorId:N}";
                    Guid seriesId;
                    if (seriesCache.TryGetValue(seriesKey, out var cachedId))
                    {
                        seriesId = cachedId;
                    }
                    else
                    {
                        seriesId = DeriveId("series|" + seriesKey);
                        var existingSeries = await seriesRepo.FindByIdAsync(seriesId);
                        if (existingSeries is null)
                        {
                            var newSeries = Series.CreateWithId(seriesId, item.RawTitle, normalizedTitle, publisherId);
                            if (primaryAuthorId.HasValue)
                                newSeries.SetPrimaryAuthor(primaryAuthorId.Value);
                            for (var i = 0; i < authorIds.Count; i++)
                            {
                                var role = i == 0 ? SeriesAuthorRole.Primary : SeriesAuthorRole.Co;
                                newSeries.AddSeriesAuthor(SeriesAuthor.Create(seriesId, authorIds[i], role));
                            }
                            await seriesRepo.UpsertAsync(newSeries);
                        }
                        seriesCache[seriesKey] = seriesId;
                    }

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
                LogUpsertFailed(logger, item.Isbn13 ?? "(null)", ex);
                var itemKey = string.IsNullOrWhiteSpace(item.Isbn13) ? "(no-isbn)" : item.Isbn13;
                if (!string.IsNullOrWhiteSpace(item.Isbn13))
                    failedIsbns.Add(item.Isbn13);
                await batchRunRepo.AddFailedItemAsync(
                    FailedItem.Create(input.BatchRunId, itemKey, ex.Message));
            }
        }

        return new UpsertVolumesOutput(upserted, thumbnailPending, failedIsbns);
    }

    private async Task<List<Guid>> ResolveAuthorIdsAsync(
        string? authorRaw, Dictionary<string, Guid> cache)
    {
        var ids = new List<Guid>();
        if (string.IsNullOrWhiteSpace(authorRaw)) return ids;

        // Rakuten author field may contain multiple names separated by ／/、/, etc.
        var names = authorRaw
            .Split(['／', '/', '、', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var rawName in names)
        {
            var name = rawName.Length > 128 ? rawName[..128] : rawName;
            string normalized;
            try { normalized = TitleNormalizer.Normalize(name); }
            catch { continue; }
            if (string.IsNullOrWhiteSpace(normalized)) continue;

            if (cache.TryGetValue(normalized, out var cached)) { ids.Add(cached); continue; }

            var existing = await authorRepo.FindByNormalizedNameAsync(normalized);
            Guid id;
            if (existing is not null)
            {
                id = existing.AuthorId;
            }
            else
            {
                var newAuthor = Author.CreateWithId(DeriveId("author|" + normalized), name, normalized);
                id = await authorRepo.UpsertAsync(newAuthor);
            }
            cache[normalized] = id;
            ids.Add(id);
        }
        return ids;
    }

    private async Task<Guid> ResolvePublisherIdAsync(
        string publisherName, Dictionary<string, Guid> cache)
    {
        var name = publisherName.Length > 128 ? publisherName[..128] : publisherName;
        var normalized = TitleNormalizer.Normalize(name);
        if (cache.TryGetValue(normalized, out var cached)) return cached;

        var existing = await publisherRepo.FindByNormalizedNameAsync(normalized);
        Guid id;
        if (existing is not null)
        {
            id = existing.PublisherId;
        }
        else
        {
            var newPub = Publisher.CreateWithId(DeriveId("publisher|" + normalized), name, normalized);
            id = await publisherRepo.UpsertAsync(newPub);
        }
        cache[normalized] = id;
        return id;
    }

    private static (DateTime? ReleaseDate, bool IsMonthOnly) ParseSalesDate(string? salesDate)
    {
        if (string.IsNullOrWhiteSpace(salesDate)) return (null, false);

        // Rakuten salesDate examples: "2026年04月25日", "2026年04月", "2026-04-25", "2026-04"
        var s = salesDate.Replace("頃", string.Empty).Trim();

        var m1 = System.Text.RegularExpressions.Regex.Match(s, @"^(\d{4})年(\d{1,2})月(\d{1,2})日?$");
        if (m1.Success
            && int.TryParse(m1.Groups[1].Value, out var y1)
            && int.TryParse(m1.Groups[2].Value, out var mo1)
            && int.TryParse(m1.Groups[3].Value, out var d1)
            && IsValidYmd(y1, mo1, d1))
        {
            return (new DateTime(y1, mo1, d1), false);
        }

        var m2 = System.Text.RegularExpressions.Regex.Match(s, @"^(\d{4})年(\d{1,2})月$");
        if (m2.Success
            && int.TryParse(m2.Groups[1].Value, out var y2)
            && int.TryParse(m2.Groups[2].Value, out var mo2)
            && mo2 is >= 1 and <= 12)
        {
            return (new DateTime(y2, mo2, DateTime.DaysInMonth(y2, mo2)), true);
        }

        if (DateTime.TryParseExact(s, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var iso))
            return (iso, false);

        if (DateTime.TryParseExact(s, "yyyy-MM", null,
                System.Globalization.DateTimeStyles.None, out var isoMonth))
            return (new DateTime(isoMonth.Year, isoMonth.Month,
                DateTime.DaysInMonth(isoMonth.Year, isoMonth.Month)), true);

        return (null, false);
    }

    private static bool IsValidYmd(int y, int m, int d)
        => m is >= 1 and <= 12 && d >= 1 && d <= DateTime.DaysInMonth(y, m);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid ISBN: {Isbn}")]
    private static partial void LogInvalidIsbn(ILogger logger, string isbn);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to upsert volume {Isbn}")]
    private static partial void LogUpsertFailed(ILogger logger, string isbn, Exception ex);

    private static Guid DeriveId(string key)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var lo = System.IO.Hashing.XxHash64.HashToUInt64(bytes, seed: 0);
        var hi = System.IO.Hashing.XxHash64.HashToUInt64(bytes, seed: unchecked((long)0x9E3779B97F4A7C15UL));
        Span<byte> guidBytes = stackalloc byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(guidBytes[..8], lo);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(guidBytes[8..], hi);
        return new Guid(guidBytes);
    }
}
