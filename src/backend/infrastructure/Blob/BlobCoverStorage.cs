using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComiCal.Infrastructure.Blob;

/// <summary>Azure Blob Storage を用いた <see cref="ICoverBlobStorage"/> 実装。</summary>
public sealed class BlobCoverStorage : ICoverBlobStorage
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<BlobCoverStorage> _logger;

    public BlobCoverStorage(
        BlobServiceClient serviceClient,
        IOptions<BlobStorageOptions> options,
        ILogger<BlobCoverStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceClient = serviceClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Uri> UploadAsync(
        string isbn,
        ReadOnlyMemory<byte> bytes,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var container = _serviceClient.GetBlobContainerClient(_options.PublicContainer);
        var blob = container.GetBlobClient(BuildBlobName(isbn));
        using var ms = new MemoryStream(bytes.ToArray(), writable: false);

        await blob.UploadAsync(
            ms,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000, immutable",
                },
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Uploaded cover {Isbn} to {Uri}", isbn, blob.Uri);
        return blob.Uri;
    }

    public async Task<bool> ExistsAsync(string isbn, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isbn);
        var container = _serviceClient.GetBlobContainerClient(_options.PublicContainer);
        var blob = container.GetBlobClient(BuildBlobName(isbn));
        var resp = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        return resp.Value;
    }

    public Task<byte[]> ComputeSha256Async(
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = SHA256.HashData(bytes.Span);
        return Task.FromResult(hash);
    }

    private static string BuildBlobName(string isbn) => $"{isbn}.jpg";
}
