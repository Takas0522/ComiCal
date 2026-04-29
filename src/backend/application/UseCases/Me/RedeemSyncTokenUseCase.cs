using ComiCal.Application.Common;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ComiCal.Application.UseCases.Me;

/// <summary>QR 同期トークン消費コマンド。</summary>
/// <param name="Token">QR コードに埋め込まれていたプレーンテキストのワンタイムトークン。</param>
public sealed record RedeemSyncTokenCommand(string Token);

/// <summary>QR 同期トークン消費ユースケース。</summary>
public interface IRedeemSyncTokenUseCase
{
    Task<Result> ExecuteAsync(
        RedeemSyncTokenCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IRedeemSyncTokenUseCase" />
public sealed class RedeemSyncTokenUseCase(
    IValidator<RedeemSyncTokenCommand> validator,
    ISyncTokenRepository repository,
    ILogger<RedeemSyncTokenUseCase> logger,
    TimeProvider? timeProvider = null) : IRedeemSyncTokenUseCase
{
    private readonly IValidator<RedeemSyncTokenCommand> _validator = validator;
    private readonly ISyncTokenRepository _repository = repository;
    private readonly ILogger<RedeemSyncTokenUseCase> _logger = logger;
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        RedeemSyncTokenCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure(MeErrors.AuthRequired());
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var hash = SyncToken.ComputeHash(command.Token);
        var entity = await _repository.FindByHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            // Plaintext token never logged.
            _logger.LogInformation("SyncToken redeem failed: not found (user {UserId})", userId);
            return Result.Failure(MeErrors.SyncTokenNotFound());
        }

        if (entity.ConsumedAt is not null)
        {
            _logger.LogInformation("SyncToken {SyncTokenId} already consumed (user {UserId})", entity.Id, userId);
            return Result.Failure(MeErrors.SyncTokenAlreadyConsumed());
        }

        var nowUtc = _time.GetUtcNow().UtcDateTime;
        if (entity.ExpiresAt <= nowUtc)
        {
            _logger.LogInformation("SyncToken {SyncTokenId} expired (user {UserId})", entity.Id, userId);
            return Result.Failure(MeErrors.SyncTokenExpired());
        }

        if (entity.UserId != userId)
        {
            // The redeeming user is not the same logical user that issued the token.
            _logger.LogWarning(
                "SyncToken {SyncTokenId} user mismatch (issuer {IssuerId} != redeemer {UserId})",
                entity.Id, entity.UserId, userId);
            return Result.Failure(MeErrors.SyncTokenUserMismatch());
        }

        var consumed = await _repository.MarkConsumedAsync(entity.Id, cancellationToken).ConfigureAwait(false);
        if (!consumed)
        {
            // A race: another redeem won. Treat as already-consumed.
            return Result.Failure(MeErrors.SyncTokenAlreadyConsumed());
        }

        _logger.LogInformation("SyncToken {SyncTokenId} redeemed by user {UserId}", entity.Id, userId);
        return Result.Success();
    }
}
