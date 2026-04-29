namespace ComiCal.Infrastructure.Rakuten;

/// <summary>楽天 Books API クライアント設定。<c>Rakuten</c> 構成セクションから束縛する。</summary>
public sealed class RakutenBooksOptions
{
    /// <summary>構成セクション名。</summary>
    public const string SectionName = "Rakuten";

    /// <summary>楽天 Books API のベース URL。</summary>
    public string BaseUrl { get; set; } = "https://app.rakuten.co.jp/";

    /// <summary>アプリケーション ID（Rakuten Developer から発行）。</summary>
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>アフィリエイト ID（任意）。</summary>
    public string? AffiliateId { get; set; }

    /// <summary>レート制限（リクエスト/秒）。デフォルト 1。</summary>
    public int RatePerSecond { get; set; } = 1;

    /// <summary>HttpClient タイムアウト（秒）。デフォルト 10。</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
