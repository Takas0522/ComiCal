namespace ComiCal.Infrastructure.Blob;

/// <summary>表紙画像 Blob ストレージ抽象。</summary>
public interface ICoverBlobStorage
{
    /// <summary>表紙画像をアップロードし、公開 URL を返す。</summary>
    Task<Uri> UploadAsync(
        string isbn,
        ReadOnlyMemory<byte> bytes,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>指定 ISBN の表紙画像が既に存在するか判定する。</summary>
    Task<bool> ExistsAsync(string isbn, CancellationToken cancellationToken);

    /// <summary>バイト列の SHA-256 ハッシュを計算する。</summary>
    Task<byte[]> ComputeSha256Async(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
}
