using ComiCal.Infrastructure.Rakuten.Models;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>楽天 Books API クライアント。</summary>
public interface IRakutenBooksClient
{
    /// <summary>
    /// 漫画ジャンル（<c>booksGenreId=001001</c>）でキーワード検索する。
    /// </summary>
    /// <param name="keyword">検索キーワード（必須）。</param>
    /// <param name="page">ページ番号（1 オリジン）。</param>
    Task<RakutenSearchResponse> SearchByGenreAsync(
        string keyword,
        int page,
        CancellationToken cancellationToken);

    /// <summary>ISBN-13 で 1 件取得する。該当なしは <c>null</c>。</summary>
    Task<RakutenItem?> GetByIsbnAsync(string isbn13, CancellationToken cancellationToken);
}
