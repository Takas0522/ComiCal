namespace ComiCal.Application.DTOs;

/// <summary>著者 DTO。</summary>
public sealed record AuthorDto(Guid Id, string Name);

/// <summary>出版社 DTO。</summary>
public sealed record PublisherDto(Guid Id, string Name);

/// <summary>表紙サムネイル DTO（フロントへ返却するメタデータのみ）。</summary>
public sealed record ThumbnailDto(string BlobKey, int Width, int Height);

/// <summary>巻 DTO（API レスポンス）。</summary>
public sealed record VolumeDto(
    Guid Id,
    Guid SeriesId,
    string Isbn,
    int? VolumeNumber,
    DateOnly? ReleaseDate,
    bool ReleaseDateIsMonthOnly,
    string? RakutenItemUrl,
    ThumbnailDto? Thumbnail);

/// <summary>シリーズ概要 DTO（一覧用）。</summary>
public sealed record SeriesSummaryDto(
    Guid Id,
    string Title,
    Guid? PublisherId,
    Guid PrimaryAuthorId,
    bool IsCompleted);

/// <summary>シリーズ詳細 DTO（巻一覧を含む）。</summary>
public sealed record SeriesDetailDto(
    SeriesSummaryDto Series,
    IReadOnlyList<VolumeDto> Volumes);

/// <summary>シリーズ検索結果 DTO（keyset pagination）。</summary>
public sealed record SeriesSearchResultDto(
    IReadOnlyList<SeriesSummaryDto> Items,
    string? NextCursor);

/// <summary>巻検索結果 DTO（keyset pagination）。</summary>
public sealed record VolumeSearchResultDto(
    IReadOnlyList<VolumeDto> Items,
    string? NextCursor);

/// <summary>カレンダーの 1 日分（その日に発売される巻のリスト）。</summary>
public sealed record CalendarDayDto(DateOnly Date, IReadOnlyList<VolumeDto> Volumes);

/// <summary>カレンダー DTO（指定月から N ヶ月分の発売予定を日単位でグルーピングしたもの）。</summary>
public sealed record CalendarDto(
    DateOnly MonthFrom,
    int MonthCount,
    IReadOnlyList<CalendarDayDto> Days);

/// <summary>購読 DTO（API レスポンス）。</summary>
public sealed record SubscriptionDto(
    Guid Id,
    Guid SeriesId,
    DateTime CreatedAt);

/// <summary>購読一覧レスポンス。</summary>
public sealed record SubscriptionListDto(IReadOnlyList<SubscriptionDto> Items);

/// <summary>購入 DTO（API レスポンス）。</summary>
public sealed record PurchaseDto(
    Guid Id,
    Guid VolumeId,
    string State,
    DateTime? PurchasedAt,
    DateTime CreatedAt);

/// <summary>購入一覧レスポンス。</summary>
public sealed record PurchaseListDto(IReadOnlyList<PurchaseDto> Items);

/// <summary><c>POST /api/me/sync/merge</c> リクエストボディの購読項目。</summary>
public sealed record MergeAnonymousSubscriptionItem(Guid SeriesId);

/// <summary><c>POST /api/me/sync/merge</c> リクエストボディの購入項目。</summary>
public sealed record MergeAnonymousPurchaseItem(Guid VolumeId, DateTime? PurchasedAt);

/// <summary><c>POST /api/me/sync/merge</c> リクエストボディ。</summary>
public sealed record MergeAnonymousDataRequest(
    IReadOnlyList<MergeAnonymousSubscriptionItem>? Subscriptions,
    IReadOnlyList<MergeAnonymousPurchaseItem>? Purchases);

/// <summary>マージ件数（取り込み成功）と Skip 一覧。</summary>
public sealed record MergeResultDto(
    MergeCountDto Merged,
    MergeSkippedDto Skipped);

/// <summary>マージで取り込まれた件数。</summary>
public sealed record MergeCountDto(int Subscriptions, int Purchases);

/// <summary>マージで Skip された ID 一覧（参照先が存在しない等）。</summary>
public sealed record MergeSkippedDto(
    IReadOnlyList<Guid> Subscriptions,
    IReadOnlyList<Guid> Purchases);
