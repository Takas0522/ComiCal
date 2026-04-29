using ComiCal.Application.Common;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary>購読解除コマンド。</summary>
public sealed record RemoveSubscriptionCommand(Guid SeriesId);

/// <summary>購読解除ユースケース。論理削除＋冪等。対象が存在しなければ 404。</summary>
public interface IRemoveSubscriptionUseCase
{
    Task<Result> ExecuteAsync(
        RemoveSubscriptionCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IRemoveSubscriptionUseCase" />
public sealed class RemoveSubscriptionUseCase(ISubscriptionRepository repository) : IRemoveSubscriptionUseCase
{
    private readonly ISubscriptionRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        RemoveSubscriptionCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure(MeErrors.AuthRequired());
        }
        if (command.SeriesId == Guid.Empty)
        {
            return Result.Failure(ApplicationErrors.Validation("SeriesId must not be empty."));
        }

        var deleted = await _repository.SoftDeleteAsync(userId, command.SeriesId, cancellationToken).ConfigureAwait(false);
        if (!deleted)
        {
            return Result.Failure(MeErrors.SubscriptionNotFound(command.SeriesId));
        }
        return Result.Success();
    }
}
