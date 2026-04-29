using ComiCal.Application.Common;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary>
/// アカウント削除コマンド。<see cref="UserId"/> は API レイヤーが
/// <c>ICurrentUser</c> から詰める。リクエストボディ由来の値は受け付けない。
/// </summary>
public sealed record DeleteAccountCommand(Guid UserId);

/// <summary>
/// 認証ユーザー本人によるアカウント削除ユースケース（個人情報保護法準拠のハード削除）。
/// 冪等: 既に削除済み（行が存在しない）場合は成功扱いで返す。
/// </summary>
public interface IDeleteAccountUseCase
{
    Task<Result> ExecuteAsync(
        DeleteAccountCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IDeleteAccountUseCase" />
public sealed class DeleteAccountUseCase(IUserRepository repository) : IDeleteAccountUseCase
{
    private readonly IUserRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task<Result> ExecuteAsync(
        DeleteAccountCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } contextUserId || contextUserId == Guid.Empty)
        {
            return Result.Failure(MeErrors.AuthRequired());
        }

        // Defence-in-depth: command.UserId must match the authenticated principal so a
        // future caller cannot smuggle a different id through the command record.
        if (command.UserId != contextUserId)
        {
            return Result.Failure(MeErrors.AuthRequired());
        }

        // Idempotent: HardDeleteAsync returns false when the user is already absent;
        // this is reported as success per spec (caller has no reason to distinguish).
        _ = await _repository.HardDeleteAsync(contextUserId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
