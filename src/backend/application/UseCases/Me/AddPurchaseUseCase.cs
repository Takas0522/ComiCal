using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases.Me;

/// <summary>購入登録コマンド。</summary>
public sealed record AddPurchaseCommand(Guid VolumeId, DateTime? PurchasedAt);

/// <summary>購入登録ユースケースの結果。<see cref="Created"/> = 新規挿入（201）、<c>false</c> = 既存更新（200）。</summary>
public sealed record AddPurchaseResult(PurchaseDto Purchase, bool Created);

/// <summary>購入登録ユースケース（冪等 UPSERT、(UserId, VolumeId) 一意）。</summary>
public interface IAddPurchaseUseCase
{
    Task<Result<AddPurchaseResult>> ExecuteAsync(
        AddPurchaseCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IAddPurchaseUseCase" />
public sealed class AddPurchaseUseCase(
    IValidator<AddPurchaseCommand> validator,
    IPurchaseRepository purchases,
    IVolumeRepository volumes) : IAddPurchaseUseCase
{
    private readonly IValidator<AddPurchaseCommand> _validator = validator;
    private readonly IPurchaseRepository _purchases = purchases;
    private readonly IVolumeRepository _volumes = volumes;

    /// <inheritdoc />
    public async Task<Result<AddPurchaseResult>> ExecuteAsync(
        AddPurchaseCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<AddPurchaseResult>.Failure(MeErrors.AuthRequired());
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<AddPurchaseResult>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        // 404 if the target volume does not exist.
        var volume = await _volumes.GetByIdAsync(command.VolumeId, cancellationToken).ConfigureAwait(false);
        if (volume is null)
        {
            return Result<AddPurchaseResult>.Failure(ApplicationErrors.VolumeNotFoundById(command.VolumeId));
        }

        var (entity, outcome) = await _purchases.UpsertAsync(
            userId, command.VolumeId, command.PurchasedAt, cancellationToken).ConfigureAwait(false);

        var dto = new PurchaseDto(entity.Id, entity.VolumeId, entity.State, entity.PurchasedAt, entity.CreatedAt);
        return Result<AddPurchaseResult>.Success(new AddPurchaseResult(dto, outcome == UpsertOutcome.Created));
    }
}
