using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.User;

public sealed class ResolveUserUseCase(IUserRepository userRepo)
{
    public async Task<Result<Domain.Entities.User>> ExecuteAsync(
        string provider, string subject, string displayName, CancellationToken ct = default)
    {
        var existing = await userRepo.FindByIdentityAsync(provider, subject, ct);
        if (existing is not null)
        {
            if (existing.IsDeleted) return Result.Failure<Domain.Entities.User>(Error.Unauthorized());
            return Result.Success(existing);
        }

        var identityProvider = provider.ToLowerInvariant() switch
        {
            "aad" or "microsoft" => IdentityProvider.Microsoft,
            "google" => IdentityProvider.Google,
            "twitter" => IdentityProvider.Twitter,
            _ => IdentityProvider.Microsoft
        };

        var user = Domain.Entities.User.Create(displayName);
        user.AddIdentityLink(IdentityLink.Create(user.UserId, identityProvider, subject));
        await userRepo.UpsertAsync(user, ct);
        return Result.Success(user);
    }
}
