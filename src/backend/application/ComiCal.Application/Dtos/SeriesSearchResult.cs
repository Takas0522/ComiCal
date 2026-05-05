namespace ComiCal.Application.Dtos;

/// <summary>シリーズ検索のレスポンス。DB 結果と楽天候補の両方を含む。</summary>
public sealed record SeriesSearchResult(
    IReadOnlyList<SeriesDto> Items,
    string? NextCursor,
    IReadOnlyList<RakutenCandidateDto> RakutenCandidates);
