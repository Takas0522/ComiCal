using System.Collections.Generic;

namespace ComiCal.Api.Common;

/// <summary>
/// Decoded payload of the SWA <c>x-ms-client-principal</c> header.
/// See https://learn.microsoft.com/azure/static-web-apps/user-information.
/// Phase 2 maps <see cref="UserId"/> (the IdP subject) to an internal
/// <c>Users.Id</c> (GUID) via <c>IdentityLinks</c> in a downstream middleware.
/// </summary>
public sealed record ClientPrincipal(
    string IdentityProvider,
    string UserId,
    string UserDetails,
    IReadOnlyList<string> UserRoles,
    IReadOnlyList<ClientPrincipalClaim> Claims)
{
    /// <summary>An anonymous (no SWA principal header) caller.</summary>
    public static ClientPrincipal Anonymous { get; } = new(
        IdentityProvider: string.Empty,
        UserId: string.Empty,
        UserDetails: string.Empty,
        UserRoles: new[] { "anonymous" },
        Claims: System.Array.Empty<ClientPrincipalClaim>());

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(UserId)
        && UserRoles.Contains("authenticated");
}

public sealed record ClientPrincipalClaim(string Type, string Value);
