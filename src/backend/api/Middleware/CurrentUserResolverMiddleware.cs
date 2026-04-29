using ComiCal.Api.Common;
using ComiCal.Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// Resolves the SWA <see cref="ClientPrincipal"/> (set by
/// <see cref="SwaAuthMiddleware"/>) into an internal <c>Users.UserId</c> GUID
/// and populates the scoped <see cref="ICurrentUser"/> accessor.
///
/// <list type="bullet">
///   <item>Anonymous invocations are fast-pathed: no DB round trip, accessor stays empty.</item>
///   <item>Authenticated invocations call
///         <see cref="IUserRepository.EnsureExistsAsync(string,string,System.Threading.CancellationToken)"/>
///         to UPSERT a row keyed by <c>ExternalId</c> (the SWA <c>userId</c>),
///         then mirror the result onto <see cref="ICurrentUser"/>.</item>
/// </list>
///
/// Must be registered <strong>after</strong> <see cref="SwaAuthMiddleware"/>
/// so that the principal has already been decoded and 401 short-circuits have
/// already happened for protected routes.
/// </summary>
public sealed class CurrentUserResolverMiddleware(
    ILogger<CurrentUserResolverMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private readonly ILogger<CurrentUserResolverMiddleware> _logger = logger;

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var accessor = context.InstanceServices.GetService<ICurrentUserAccessor>();
        var currentUser = context.InstanceServices.GetService<ICurrentUser>() as CurrentUser;

        if (accessor is { IsAuthenticated: true } && currentUser is not null)
        {
            var repo = context.InstanceServices.GetRequiredService<IUserRepository>();
            var ct = context.GetHttpContext()?.RequestAborted ?? context.CancellationToken;
            await ResolveAsync(accessor.Principal, repo, currentUser, _logger, ct);
        }

        await next(context);
    }

    /// <summary>
    /// Pure UPSERT-and-populate flow, exposed for unit testing without spinning
    /// up a Functions <see cref="FunctionContext"/>.
    /// </summary>
    public static async Task ResolveAsync(
        ClientPrincipal principal,
        IUserRepository userRepository,
        CurrentUser currentUser,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(logger);

        if (!principal.IsAuthenticated)
        {
            return;
        }

        var displayName = ResolveDisplayName(principal);
        var domainUser = await userRepository.EnsureExistsAsync(
            principal.UserId,
            displayName,
            cancellationToken);

        currentUser.Populate(domainUser.Id, domainUser.ExternalId, domainUser.DisplayName);
        logger.LogDebug(
            "Resolved CurrentUser {UserId} for ExternalId {ExternalId}",
            domainUser.Id,
            domainUser.ExternalId);
    }

    /// <summary>
    /// Picks a sensible non-empty display name from the SWA principal.
    /// Falls back to <c>UserDetails</c>, then <c>UserId</c>, so that the
    /// 64-char NOT NULL <c>DisplayName</c> column is always satisfied even if
    /// the IdP claim set is sparse. Exposed for unit testing.
    /// </summary>
    public static string ResolveDisplayName(ClientPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (!string.IsNullOrWhiteSpace(principal.UserDetails))
        {
            return Truncate(principal.UserDetails, 64);
        }
        if (!string.IsNullOrWhiteSpace(principal.UserId))
        {
            return Truncate(principal.UserId, 64);
        }
        return "user";
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
