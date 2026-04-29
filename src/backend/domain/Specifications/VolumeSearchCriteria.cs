namespace ComiCal.Domain.Specifications;

/// <summary>
/// 巻検索条件。<c>Query</c> はひらがな正規化済みの検索キーワードを想定する。
/// keyset pagination のカーソルは <c>(CursorReleaseDate, CursorVolumeId)</c>。
/// </summary>
public sealed record VolumeSearchCriteria(
    string? Query,
    DateOnly? ReleaseFrom,
    DateOnly? ReleaseTo,
    Guid? PublisherId,
    int Limit,
    DateOnly? CursorReleaseDate,
    Guid? CursorVolumeId);
