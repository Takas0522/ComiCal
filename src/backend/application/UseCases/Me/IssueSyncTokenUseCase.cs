using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using Microsoft.Extensions.Logging;

namespace ComiCal.Application.UseCases.Me;

/// <summary>QR 同期トークン発行コマンド。</summary>
/// <param name="QrBaseUrl">QR ペイロードに埋め込むベース URL（例: <c>https://comical.example</c>）。
/// API レイヤがリクエストの origin（X-Forwarded-Host）を解決して渡す。末尾スラッシュは無視する。</param>
public sealed record IssueSyncTokenCommand(string QrBaseUrl);

/// <summary>QR 同期トークン発行ユースケース。</summary>
public interface IIssueSyncTokenUseCase
{
    Task<Result<SyncTokenIssuedDto>> ExecuteAsync(
        IssueSyncTokenCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IIssueSyncTokenUseCase" />
public sealed class IssueSyncTokenUseCase(
    ISyncTokenRepository repository,
    ILogger<IssueSyncTokenUseCase> logger,
    TimeProvider? timeProvider = null) : IIssueSyncTokenUseCase
{
    /// <summary>仕様の既定 TTL（5 分）。</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly ISyncTokenRepository _repository = repository;
    private readonly ILogger<IssueSyncTokenUseCase> _logger = logger;
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    public async Task<Result<SyncTokenIssuedDto>> ExecuteAsync(
        IssueSyncTokenCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<SyncTokenIssuedDto>.Failure(MeErrors.AuthRequired());
        }
        if (string.IsNullOrWhiteSpace(command.QrBaseUrl))
        {
            return Result<SyncTokenIssuedDto>.Failure(
                ApplicationErrors.Validation("QrBaseUrl must be provided by the API layer."));
        }

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        var (entity, plaintext) = SyncToken.CreateNew(Guid.CreateVersion7(), userId, nowUtc, DefaultTtl);
        await _repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        // Never log plaintext; tokenId only.
        _logger.LogInformation("SyncToken {SyncTokenId} issued for user {UserId}", entity.Id, userId);

        var baseUrl = command.QrBaseUrl.TrimEnd('/');
        var qrPayload = $"{baseUrl}/sync?token={Uri.EscapeDataString(plaintext)}";
        return Result<SyncTokenIssuedDto>.Success(new SyncTokenIssuedDto(plaintext, entity.ExpiresAt, qrPayload));
    }
}
