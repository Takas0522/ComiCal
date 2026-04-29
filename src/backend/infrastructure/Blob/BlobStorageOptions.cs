namespace ComiCal.Infrastructure.Blob;

/// <summary>Blob Storage 構成。<c>Storage</c> セクションから束縛する。</summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Storage account URI（例: <c>https://comicalprod.blob.core.windows.net</c>）。Managed Identity で接続する場合に使う。</summary>
    public string? AccountUri { get; set; }

    /// <summary>開発時の接続文字列（Azurite 等）。<see cref="AccountUri"/> が空のときに使用。</summary>
    public string? ConnectionString { get; set; }

    /// <summary>公開コンテナ名（既定: <c>covers-public</c>）。</summary>
    public string PublicContainer { get; set; } = "covers-public";

    /// <summary>非公開コンテナ名（バッチが使用、既定: <c>covers-staging</c>）。</summary>
    public string StagingContainer { get; set; } = "covers-staging";
}
