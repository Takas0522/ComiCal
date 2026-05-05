using ComiCal.Application.Interfaces;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;

namespace ComiCal.Application.Services;

/// <summary>
/// 楽天 Books の書誌情報を Series / Author / Publisher / Volume として UPSERT する共有サービス。
/// UpsertVolumesActivity（バッチ）と AddSubscriptionFromRakutenUseCase（API）から利用される。
/// </summary>
public sealed class SeriesUpsertService(
    ISeriesRepository seriesRepo,
    IAuthorRepository authorRepo,
    IPublisherRepository publisherRepo,
    IVolumeRepository volumeRepo)
{
    /// <summary>
    /// 楽天 Books の書誌情報を UPSERT し、該当 Series の ID を返します。
    /// </summary>
    public async Task<Guid> UpsertAsync(RakutenBookSearchItem item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var authorIds = await ResolveAuthorIdsAsync(item.Author, ct);
        var primaryAuthorId = authorIds.Count > 0 ? authorIds[0] : (Guid?)null;

        Guid? publisherId = null;
        if (!string.IsNullOrWhiteSpace(item.PublisherName))
            publisherId = await ResolvePublisherIdAsync(item.PublisherName, ct);

        var normalizedTitle = SeriesAggregator.ComputeNormalizedTitle(item.Title);
        var seriesKey = $"{normalizedTitle}|{primaryAuthorId:N}";
        var seriesId = DeriveId("series|" + seriesKey);

        var existingSeries = await seriesRepo.FindByIdAsync(seriesId, ct);
        if (existingSeries is null)
        {
            var newSeries = Series.CreateWithId(seriesId, item.Title, normalizedTitle, publisherId);
            if (primaryAuthorId.HasValue)
                newSeries.SetPrimaryAuthor(primaryAuthorId.Value);
            for (var i = 0; i < authorIds.Count; i++)
            {
                var role = i == 0 ? SeriesAuthorRole.Primary : SeriesAuthorRole.Co;
                newSeries.AddSeriesAuthor(SeriesAuthor.Create(seriesId, authorIds[i], role));
            }
            await seriesRepo.UpsertAsync(newSeries, ct);
        }

        // Upsert volume if ISBN is valid
        if (!string.IsNullOrWhiteSpace(item.Isbn)
            && item.Isbn.Length == 13
            && item.Isbn.All(char.IsDigit))
        {
            var existing = await volumeRepo.FindByIsbnAsync(item.Isbn, ct);
            if (existing is null)
            {
                var (releaseDate, isMonthOnly) = ParseSalesDate(item.SalesDate);
                var volumeNumber = VolumeNumberExtractor.Extract(item.Title);
                var volume = Volume.Create(seriesId, item.Isbn, volumeNumber, releaseDate, isMonthOnly);
                volume.UpdateRakutenItemUrl(item.ItemUrl);
                await volumeRepo.UpsertAsync(volume, ct);
            }
            else
            {
                existing.UpdateRakutenItemUrl(item.ItemUrl);
                await volumeRepo.UpsertAsync(existing, ct);
            }
        }

        return seriesId;
    }

    private async Task<List<Guid>> ResolveAuthorIdsAsync(string? authorRaw, CancellationToken ct)
    {
        var ids = new List<Guid>();
        if (string.IsNullOrWhiteSpace(authorRaw)) return ids;

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
            catch (ArgumentException) { continue; }
            if (string.IsNullOrWhiteSpace(normalized)) continue;

            var existing = await authorRepo.FindByNormalizedNameAsync(normalized, ct);
            Guid id;
            if (existing is not null)
            {
                id = existing.AuthorId;
            }
            else
            {
                var newAuthor = Author.CreateWithId(DeriveId("author|" + normalized), name, normalized);
                id = await authorRepo.UpsertAsync(newAuthor, ct);
            }
            ids.Add(id);
        }
        return ids;
    }

    private async Task<Guid> ResolvePublisherIdAsync(string publisherName, CancellationToken ct)
    {
        var name = publisherName.Length > 128 ? publisherName[..128] : publisherName;
        var normalized = TitleNormalizer.Normalize(name);

        var existing = await publisherRepo.FindByNormalizedNameAsync(normalized, ct);
        if (existing is not null)
            return existing.PublisherId;

        var newPub = Publisher.CreateWithId(DeriveId("publisher|" + normalized), name, normalized);
        return await publisherRepo.UpsertAsync(newPub, ct);
    }

    internal static (DateTime? ReleaseDate, bool IsMonthOnly) ParseSalesDate(string? salesDate)
    {
        if (string.IsNullOrWhiteSpace(salesDate)) return (null, false);

        var s = salesDate.Replace("頃", string.Empty).Trim();

        var m1 = System.Text.RegularExpressions.Regex.Match(s, @"^(\d{4})年(\d{1,2})月(\d{1,2})日?$");
        if (m1.Success
            && int.TryParse(m1.Groups[1].Value, out var y1)
            && int.TryParse(m1.Groups[2].Value, out var mo1)
            && int.TryParse(m1.Groups[3].Value, out var d1)
            && mo1 is >= 1 and <= 12 && d1 >= 1 && d1 <= DateTime.DaysInMonth(y1, mo1))
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

    internal static Guid DeriveId(string key)
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
