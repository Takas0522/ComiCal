using ComiCal.Batch.Internal;
using ComiCal.Batch.Models;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.ValueObjects;
using ComiCal.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Idempotent UPSERT of a single Rakuten payload into Publishers / Authors / Series /
/// SeriesAuthors / Volumes. Wrapped in a single SQL Server transaction so concurrent
/// activity invocations cannot leave partial state.
/// </summary>
public sealed class UpsertSeriesAndVolumeActivity(
    ComiCalDbContext db,
    IVolumeRepository volumeRepository,
    ILogger<UpsertSeriesAndVolumeActivity> logger)
{
    private readonly ComiCalDbContext _db = db;
    private readonly IVolumeRepository _volumeRepository = volumeRepository;
    private readonly ILogger<UpsertSeriesAndVolumeActivity> _logger = logger;

    [Function("UpsertSeriesAndVolume")]
    public async Task<UpsertResult> RunAsync(
        [ActivityTrigger] BatchVolumePayload payload,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        await using var tx = await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var publisherName = string.IsNullOrWhiteSpace(payload.PublisherName) ? "(不明)" : payload.PublisherName;
        var authorName = BatchTextNormalizer.ResolvePrimaryAuthorName(payload.AuthorName);
        var seriesTitle = string.IsNullOrWhiteSpace(payload.SeriesName) ? payload.Title : payload.SeriesName;

        var publisherId = await UpsertPublisherAsync(publisherName, ct).ConfigureAwait(false);
        var authorId = await UpsertAuthorAsync(authorName, ct).ConfigureAwait(false);
        var seriesId = await UpsertSeriesAsync(seriesTitle, publisherId, authorId, ct).ConfigureAwait(false);
        await UpsertSeriesAuthorAsync(seriesId, authorId, ct).ConfigureAwait(false);

        var isbn = Isbn13.Create(payload.Isbn);
        var existing = await _volumeRepository.GetByIsbnAsync(isbn, ct).ConfigureAwait(false);
        var isNew = existing is null;
        var existingHash = (existing is not null && !existing.CoverHash.IsEmpty)
            ? existing.CoverHash.ToArray()
            : null;

        // Preserve existing cover hash; EnsureCoverThumbnail re-computes it on download.
        var coverHash = existingHash is null ? ReadOnlyMemory<byte>.Empty : (ReadOnlyMemory<byte>)existingHash;

        var volume = Volume.Create(
            seriesId,
            isbn,
            payload.VolumeNumber,
            payload.ReleaseDate,
            payload.ReleaseDateIsMonthOnly,
            coverHash,
            payload.ItemUrl);

        await _volumeRepository.UpsertAsync(volume, ct).ConfigureAwait(false);

        var afterUpsert = await _volumeRepository.GetByIsbnAsync(isbn, ct).ConfigureAwait(false);
        var volumeId = afterUpsert?.Id ?? volume.Id;

        await tx.CommitAsync(ct).ConfigureAwait(false);

        _logger.LogDebug("Upserted Volume {Isbn} (IsNew={IsNew})", payload.Isbn, isNew);

        return new UpsertResult(
            VolumeId: volumeId,
            IsNew: isNew,
            Isbn: payload.Isbn,
            CoverUrl: payload.CoverImageUrl,
            CurrentCoverHash: existingHash);
    }

    private static string ResolvePrimaryAuthorName(string raw) =>
        BatchTextNormalizer.ResolvePrimaryAuthorName(raw);

    private static string Normalize(string raw) =>
        BatchTextNormalizer.Normalize(raw);

    private async Task<Guid> UpsertPublisherAsync(string name, CancellationToken ct)
    {
        const string sql = @"
SET NOCOUNT ON;
DECLARE @id uniqueidentifier;
SELECT @id = PublisherId FROM dbo.Publishers WHERE NormalizedName = @NormalizedName AND IsDeleted = 0;
IF @id IS NULL
BEGIN
    SET @id = @NewId;
    INSERT INTO dbo.Publishers (PublisherId, Name, NormalizedName, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@id, @Name, @NormalizedName, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END
SELECT @id;";
        return await ExecuteScalarGuidAsync(sql, name, Normalize(name), ct).ConfigureAwait(false);
    }

    private async Task<Guid> UpsertAuthorAsync(string name, CancellationToken ct)
    {
        const string sql = @"
SET NOCOUNT ON;
DECLARE @id uniqueidentifier;
SELECT @id = AuthorId FROM dbo.Authors WHERE NormalizedName = @NormalizedName AND IsDeleted = 0;
IF @id IS NULL
BEGIN
    SET @id = @NewId;
    INSERT INTO dbo.Authors (AuthorId, Name, NormalizedName, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@id, @Name, @NormalizedName, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END
SELECT @id;";
        return await ExecuteScalarGuidAsync(sql, name, Normalize(name), ct).ConfigureAwait(false);
    }

    private async Task<Guid> UpsertSeriesAsync(string title, Guid publisherId, Guid primaryAuthorId, CancellationToken ct)
    {
        const string sql = @"
SET NOCOUNT ON;
DECLARE @id uniqueidentifier;
SELECT @id = SeriesId
  FROM dbo.Series
 WHERE NormalizedTitle = @NormalizedTitle
   AND PrimaryAuthorId = @PrimaryAuthorId
   AND IsDeleted = 0;
IF @id IS NULL
BEGIN
    SET @id = @NewId;
    INSERT INTO dbo.Series
        (SeriesId, Title, NormalizedTitle, PublisherId, PrimaryAuthorId, IsCompleted, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
        (@id, @Title, @NormalizedTitle, @PublisherId, @PrimaryAuthorId, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END
SELECT @id;";

        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
        }
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Title", title));
        cmd.Parameters.Add(new SqlParameter("@NormalizedTitle", Normalize(title)));
        cmd.Parameters.Add(new SqlParameter("@PublisherId", publisherId));
        cmd.Parameters.Add(new SqlParameter("@PrimaryAuthorId", primaryAuthorId));
        cmd.Parameters.Add(new SqlParameter("@NewId", Guid.CreateVersion7()));
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return (Guid)result!;
    }

    private async Task UpsertSeriesAuthorAsync(Guid seriesId, Guid authorId, CancellationToken ct)
    {
        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.SeriesAuthors WHERE SeriesId = @SeriesId AND AuthorId = @AuthorId)
BEGIN
    INSERT INTO dbo.SeriesAuthors (SeriesAuthorId, SeriesId, AuthorId, Role, IsDeleted, CreatedAt, UpdatedAt)
    VALUES (@NewId, @SeriesId, @AuthorId, N'Primary', 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END";
        await _db.Database.ExecuteSqlRawAsync(
            sql,
            [
                new SqlParameter("@SeriesId", seriesId),
                new SqlParameter("@AuthorId", authorId),
                new SqlParameter("@NewId", Guid.CreateVersion7()),
            ],
            ct).ConfigureAwait(false);
    }

    private async Task<Guid> ExecuteScalarGuidAsync(string sql, string name, string normalized, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
        }
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@Name", name));
        cmd.Parameters.Add(new SqlParameter("@NormalizedName", normalized));
        cmd.Parameters.Add(new SqlParameter("@NewId", Guid.CreateVersion7()));
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return (Guid)result!;
    }
}
