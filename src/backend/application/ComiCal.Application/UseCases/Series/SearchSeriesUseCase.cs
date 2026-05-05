using ComiCal.Application.Dtos;
using ComiCal.Application.Interfaces;
using ComiCal.Application.Mappings;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using Microsoft.Extensions.Logging;

namespace ComiCal.Application.UseCases.Series;

public sealed partial class SearchSeriesUseCase(
    ISeriesRepository seriesRepo,
    IRakutenBookSearchService rakutenSearch,
    ILogger<SearchSeriesUseCase> logger)
{
    /// DB 結果がこの件数未満の場合に楽天フォールバック検索を実行する閾値。
    private const int RakutenFallbackThreshold = 20;

    /// 楽天 API 呼び出しのタイムアウト (ms)。
    private const int RakutenTimeoutMs = 3000;

    public async Task<Result<SeriesSearchResult>> ExecuteAsync(
        SeriesSearchQuery query, string? blobBaseUrl, CancellationToken ct = default)
    {
        var (items, nextCursor) = await seriesRepo.SearchAsync(query, ct);
        var dtos = items.Select(s => SeriesMapper.ToDto(s, blobBaseUrl)).ToList();

        // キーワード検索でかつ DB 結果が閾値未満の場合は楽天フォールバックを試みる
        var rakutenCandidates = new List<RakutenCandidateDto>();
        if (!string.IsNullOrWhiteSpace(query.Q) && dtos.Count < RakutenFallbackThreshold)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(RakutenTimeoutMs);

                var rakutenItems = await rakutenSearch.SearchByKeywordAsync(query.Q, cts.Token);
                rakutenCandidates = BuildRakutenCandidates(rakutenItems, dtos);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // 楽天 API タイムアウト: DB 結果のみ返す（劣化動作）
                LogRakutenTimeout(logger, query.Q);
            }
            catch (Exception ex)
            {
                // 楽天 API 障害時も検索自体は成功として返す
                LogRakutenError(logger, query.Q, ex);
            }
        }

        return Result.Success(new SeriesSearchResult(dtos, nextCursor, rakutenCandidates));
    }

    private static List<RakutenCandidateDto> BuildRakutenCandidates(
        IReadOnlyList<RakutenBookSearchItem> rakutenItems,
        IReadOnlyList<SeriesDto> dbResults)
    {
        // DB 結果のタイトル正規化セット（重複排除用）
        var dbNormalizedTitles = new HashSet<string>(
            dbResults.Select(d => NormalizeSafe(d.Title)),
            StringComparer.OrdinalIgnoreCase);

        // 楽天結果から DB にないものだけを候補として返す（ISBN ごとに 1 件）
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<RakutenCandidateDto>();

        foreach (var item in rakutenItems)
        {
            var normalizedTitle = NormalizeSafe(item.Title);
            // DB 結果と正規化タイトルが一致するものは除外
            if (dbNormalizedTitles.Contains(normalizedTitle))
                continue;

            // ISBN を dedup キーとして使用
            var key = string.IsNullOrWhiteSpace(item.Isbn) ? normalizedTitle : item.Isbn;
            if (!seen.Add(key))
                continue;

            candidates.Add(new RakutenCandidateDto(
                item.Isbn,
                item.Title,
                item.Author,
                item.PublisherName,
                item.LargeImageUrl,
                item.ItemUrl));
        }

        return candidates;
    }

    private static string NormalizeSafe(string title)
    {
        try { return SeriesAggregator.ComputeNormalizedTitle(title); }
        catch { return title.Trim().ToLowerInvariant(); }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rakuten Books keyword search timed out for query: {Query}")]
    private static partial void LogRakutenTimeout(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rakuten Books keyword search failed for query: {Query}")]
    private static partial void LogRakutenError(ILogger logger, string query, Exception ex);
}
