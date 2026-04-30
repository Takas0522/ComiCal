using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Account;

public sealed class DeleteAccountUseCase(IUserRepository userRepo)
{
    public async Task<Result<bool>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepo.FindByIdAsync(userId, ct);
        if (user is null) return Result.Failure<bool>(Error.NotFound("User"));
        await userRepo.SoftDeleteAsync(userId, ct);
        return Result.Success(true);
    }
}
