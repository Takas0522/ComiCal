namespace ComiCal.Application.Dtos;

/// <summary>楽天 Books からのみ見つかったシリーズ候補（DB に未登録）。</summary>
public sealed record RakutenCandidateDto(
    string Isbn,
    string Title,
    string? Author,
    string? PublisherName,
    string? ThumbnailUrl,
    string? ItemUrl);
