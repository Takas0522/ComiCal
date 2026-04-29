namespace ComiCal.Domain.Specifications;

/// <summary>
/// シリーズ検索条件。<c>Query</c> はひらがな正規化済みの検索キーワードを想定する。
/// keyset pagination のカーソルは <see cref="CursorSeriesId"/>。
/// </summary>
public sealed record SeriesSearchCriteria(
    string? Query,
    Guid? PublisherId,
    Guid? AuthorId,
    int Limit,
    Guid? CursorSeriesId);
