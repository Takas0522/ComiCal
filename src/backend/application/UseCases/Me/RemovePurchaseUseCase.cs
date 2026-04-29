using ComiCal.Application.Common;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary>購入解除（記録削除）コマンド。</summary>
public sealed record RemovePurchaseCommand(Guid VolumeId);

/// <summary>購入解除ユースケース。論理削除＋冪等。対象が存在しなければ 404。</summary>
public interface IRemovePurchaseUseCase
{
    Task<Result> ExecuteAsync(
        RemovePurchaseCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IRemovePurchaseUseCase" />
public sealed class RemovePurchaseUseCase(IPurchaseRepository repository) : IRemovePurchaseUseCase
{
    private readonly IPurchaseRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        RemovePurchaseCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure(MeErrors.AuthRequired());
        }
        if (command.VolumeId == Guid.Empty)
        {
            return Result.Failure(ApplicationErrors.Validation("VolumeId must not be empty."));
        }

        var deleted = await _repository.SoftDeleteAsync(userId, command.VolumeId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
        {
            return Result.Failure(MeErrors.PurchaseNotFound(command.VolumeId));
        }
        return Result.Success();
    }
}
