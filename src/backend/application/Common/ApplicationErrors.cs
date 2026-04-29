using ComiCal.Shared;

namespace ComiCal.Application.Common;

/// <summary>
/// アプリケーション層で発生する代表的なエラーのファクトリ。
/// RFC 7807 problem types とコード文字列を一元管理する。
/// </summary>
public static class ApplicationErrors
{
    /// <summary>入力バリデーション失敗（HTTP 400）。</summary>
    public static Error Validation(string message)
        => Error.Validation("validation", message);

    /// <summary>シリーズが存在しない（HTTP 404）。</summary>
    public static Error SeriesNotFound(Guid seriesId)
        => Error.NotFound("series-not-found", $"Series '{seriesId}' was not found.");

    /// <summary>巻が存在しない（HTTP 404、内部 ID 指定時）。</summary>
    public static Error VolumeNotFoundById(Guid volumeId)
        => Error.NotFound("volume-not-found", $"Volume '{volumeId}' was not found.");

    /// <summary>巻が存在しない（HTTP 404）。</summary>
    public static Error VolumeNotFound(string isbn)
        => Error.NotFound("volume-not-found", $"Volume with ISBN '{isbn}' was not found.");

    /// <summary>ISBN-13 が不正（HTTP 400）。</summary>
    public static Error InvalidIsbn(string detail)
        => Error.Validation("invalid-isbn", detail);
}
