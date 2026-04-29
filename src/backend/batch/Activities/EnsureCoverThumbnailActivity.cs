using ComiCal.Batch.Models;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Downloads the cover image for a single volume, computes its SHA-256, uploads to
/// the public Blob container if changed, and UPSERTs <c>ThumbnailAssets</c>
/// + <c>Volumes.CoverHash</c>. Skipped (cache hit) when the new hash matches the
/// previously stored hash.
/// </summary>
public sealed class EnsureCoverThumbnailActivity(
    IHttpClientFactory httpClientFactory,
    ICoverBlobStorage coverStorage,
    ComiCalDbContext db,
    ILogger<EnsureCoverThumbnailActivity> logger)
{
    public const string CoverDownloaderClientName = "cover-downloader";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ICoverBlobStorage _coverStorage = coverStorage;
    private readonly ComiCalDbContext _db = db;
    private readonly ILogger<EnsureCoverThumbnailActivity> _logger = logger;

    [Function("EnsureCoverThumbnail")]
    public async Task RunAsync(
        [ActivityTrigger] CoverDownloadInput input,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        if (string.IsNullOrWhiteSpace(input.CoverUrl))
        {
            _logger.LogDebug("Skipping {Isbn}: no cover URL", input.Isbn);
            return;
        }

        var http = _httpClientFactory.CreateClient(CoverDownloaderClientName);
        using var response = await http.GetAsync(input.CoverUrl, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

        var newHash = await _coverStorage.ComputeSha256Async(bytes, ct).ConfigureAwait(false);

        if (input.CurrentCoverHash is { Length: > 0 } && newHash.AsSpan().SequenceEqual(input.CurrentCoverHash))
        {
            _logger.LogDebug("Cover {Isbn} unchanged (hash match), skipping upload", input.Isbn);
            return;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var uri = await _coverStorage.UploadAsync(input.Isbn, bytes, contentType, ct).ConfigureAwait(false);
        var blobKey = $"{input.Isbn}.jpg";

        await UpsertThumbnailAssetAsync(input.VolumeId, blobKey, bytes.LongLength, newHash, ct).ConfigureAwait(false);
        await UpdateVolumeCoverHashAsync(input.Isbn, newHash, ct).ConfigureAwait(false);

        _logger.LogInformation("Cover {Isbn} uploaded ({Bytes} bytes) to {Uri}", input.Isbn, bytes.LongLength, uri);
    }

    private async Task UpsertThumbnailAssetAsync(
        Guid volumeId,
        string blobKey,
        long sizeBytes,
        byte[] hash,
        CancellationToken ct)
    {
        const string sql = @"
MERGE dbo.ThumbnailAssets WITH (HOLDLOCK) AS target
USING (SELECT @VolumeId AS VolumeId) AS src
   ON target.VolumeId = src.VolumeId
WHEN MATCHED THEN
    UPDATE SET BlobKey = @BlobKey, SizeBytes = @SizeBytes, ContentHash = @ContentHash,
               Width = 0, Height = 0, IsDeleted = 0, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (VolumeId, BlobKey, SizeBytes, ContentHash, Width, Height, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@VolumeId, @BlobKey, @SizeBytes, @ContentHash, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());";
        await _db.Database.ExecuteSqlRawAsync(
            sql,
            [
                new SqlParameter("@VolumeId", volumeId),
                new SqlParameter("@BlobKey", blobKey),
                new SqlParameter("@SizeBytes", sizeBytes),
                new SqlParameter("@ContentHash", hash),
            ],
            ct).ConfigureAwait(false);
    }

    private async Task UpdateVolumeCoverHashAsync(string isbn, byte[] hash, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.Volumes
   SET CoverHash = @Hash, UpdatedAt = SYSUTCDATETIME()
 WHERE Isbn13 = @Isbn AND IsDeleted = 0;";
        await _db.Database.ExecuteSqlRawAsync(
            sql,
            [
                new SqlParameter("@Hash", hash),
                new SqlParameter("@Isbn", isbn),
            ],
            ct).ConfigureAwait(false);
    }
}
