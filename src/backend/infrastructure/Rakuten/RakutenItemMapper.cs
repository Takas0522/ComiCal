using System.Globalization;
using System.Text.RegularExpressions;
using ComiCal.Domain.Entities;
using ComiCal.Domain.ValueObjects;
using ComiCal.Infrastructure.Rakuten.Models;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// 楽天 Books API のレスポンスから取り込み用 DTO（<see cref="RakutenIngestPayload"/>）を生成する純粋関数群。
/// </summary>
/// <remarks>
/// このクラスは I/O を一切行わず、入力 <see cref="RakutenItem"/> から ISBN 検証・発売日パース・巻数抽出を行う。
/// バッチの取込（Phase 2）でリポジトリ UPSERT のソースとして使う想定。
/// </remarks>
public static class RakutenItemMapper
{
    private static readonly Regex VolumeNumberRegex =
        new(@"(\d{1,4})\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] SalesDateFormats =
    [
        "yyyy年MM月dd日",
        "yyyy年M月d日",
        "yyyy年MM月",
        "yyyy年M月",
    ];

    /// <summary>
    /// 楽天アイテムを取り込みペイロードに変換する。ISBN-13 不正の場合は <c>null</c>。
    /// </summary>
    public static RakutenIngestPayload? TryMap(RakutenItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!Isbn13.TryCreate(item.Isbn, out var isbn, out _) || isbn is null)
        {
            return null;
        }

        var (releaseDate, isMonthOnly) = ParseSalesDate(item.SalesDate);
        var volumeNumber = ExtractVolumeNumber(item.Title);

        return new RakutenIngestPayload(
            Isbn: isbn,
            Title: item.Title,
            SeriesName: string.IsNullOrWhiteSpace(item.SeriesName) ? item.Title : item.SeriesName,
            SeriesNameKana: item.SeriesNameKana ?? string.Empty,
            VolumeNumber: volumeNumber,
            ReleaseDate: releaseDate,
            ReleaseDateIsMonthOnly: isMonthOnly,
            AuthorName: item.Author ?? string.Empty,
            AuthorKana: item.AuthorKana ?? string.Empty,
            PublisherName: item.PublisherName ?? string.Empty,
            ItemUrl: item.ItemUrl,
            CoverImageUrl: item.LargeImageUrl ?? item.MediumImageUrl ?? item.SmallImageUrl);
    }

    public static (DateOnly? Date, bool IsMonthOnly) ParseSalesDate(string? salesDate)
    {
        if (string.IsNullOrWhiteSpace(salesDate))
        {
            return (null, false);
        }
        foreach (var fmt in SalesDateFormats)
        {
            if (DateTime.TryParseExact(
                    salesDate,
                    fmt,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                if (fmt.EndsWith("月", StringComparison.Ordinal))
                {
                    var lastDay = DateTime.DaysInMonth(dt.Year, dt.Month);
                    return (new DateOnly(dt.Year, dt.Month, lastDay), true);
                }
                return (DateOnly.FromDateTime(dt), false);
            }
        }
        return (null, false);
    }

    public static int? ExtractVolumeNumber(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }
        var m = VolumeNumberRegex.Match(title);
        if (!m.Success)
        {
            return null;
        }
        return int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }
}

/// <summary>
/// 楽天アイテムから抽出した取込候補。ID は呼び出し側（バッチの UseCase / Repository）で解決する。
/// </summary>
public sealed record RakutenIngestPayload(
    Isbn13 Isbn,
    string Title,
    string SeriesName,
    string SeriesNameKana,
    int? VolumeNumber,
    DateOnly? ReleaseDate,
    bool ReleaseDateIsMonthOnly,
    string AuthorName,
    string AuthorKana,
    string PublisherName,
    string? ItemUrl,
    string? CoverImageUrl);
