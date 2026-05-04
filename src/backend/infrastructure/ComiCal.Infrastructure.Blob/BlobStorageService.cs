using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComiCal.Domain.Entities;
using System.Security.Cryptography;

namespace ComiCal.Infrastructure.Blob;

public sealed class BlobStorageService(BlobServiceClient blobServiceClient)
{
    private const string CoversContainer = "covers";
    private const string SyncTmpContainer = "sync-tmp";

    public async Task<ThumbnailAsset?> UploadThumbnailIfChangedAsync(
        Guid volumeId, string imageUrl, byte[]? existingHash, CancellationToken ct = default)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(imageUrl, ct);
        if (!response.IsSuccessStatusCode) return null;

        var imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
        var hash = SHA256.HashData(imageBytes);

        if (existingHash is not null && hash.SequenceEqual(existingHash))
            return null; // unchanged

        var blobName = Convert.ToHexString(hash).ToLowerInvariant() + ".jpg";
        // BlobKey is the path within the container (e.g. "<hash>.jpg").
        // The container name ("covers") is already part of BlobBaseUrl, so do NOT
        // include it here — otherwise the public URL becomes ".../covers/covers/...".
        var blobKey = blobName;
        var container = blobServiceClient.GetBlobContainerClient(CoversContainer);
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            new BinaryData(imageBytes),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg",
                    CacheControl = "public, max-age=2592000, immutable",
                },
            }, ct);

        return ThumbnailAsset.Create(volumeId, blobKey, imageBytes.LongLength, hash, 0, 0);
    }

    public async Task<(string Token, DateTime ExpiresAt)> UploadSyncQrDataAsync(
        string encryptedPayload, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        var container = blobServiceClient.GetBlobContainerClient(SyncTmpContainer);
        var blob = container.GetBlobClient(token);

        await blob.UploadAsync(BinaryData.FromString(encryptedPayload), overwrite: true, ct);

        return (token, DateTime.UtcNow.AddMinutes(5));
    }

    public async Task<string?> GetSyncQrDataAsync(string token, CancellationToken ct = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(SyncTmpContainer);
        var blob = container.GetBlobClient(token);

        if (!await blob.ExistsAsync(ct)) return null;

        var response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }
}
