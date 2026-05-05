namespace ComiCal.Application.Interfaces;

/// <summary>楽天 Books API から書誌情報を取得するサービスの抽象。</summary>
public interface IRakutenBookSearchService
{
    /// <summary>キーワードで楽天 Books を検索します（ジャンル無指定）。</summary>
    Task<IReadOnlyList<RakutenBookSearchItem>> SearchByKeywordAsync(string keyword, CancellationToken ct = default);

    /// <summary>ISBN-13 で楽天 Books を検索します。</summary>
    Task<RakutenBookSearchItem?> SearchByIsbnAsync(string isbn13, CancellationToken ct = default);
}

/// <summary>楽天 Books API から取得した書誌情報。</summary>
public sealed record RakutenBookSearchItem(
    string Isbn,
    string Title,
    string? Author,
    string? PublisherName,
    string? SalesDate,
    string? LargeImageUrl,
    string? ItemUrl);
