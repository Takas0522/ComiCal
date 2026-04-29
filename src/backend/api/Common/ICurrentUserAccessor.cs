namespace ComiCal.Api.Common;

/// <summary>
/// Scoped accessor that exposes the decoded SWA client principal for the
/// current Function invocation. Populated by <c>SwaAuthMiddleware</c>;
/// consumed by use cases / repositories that need the caller identity.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>The principal for this invocation. Never <c>null</c>; falls back to <see cref="ClientPrincipal.Anonymous"/>.</summary>
    ClientPrincipal Principal { get; }

    /// <summary><c>true</c> if the caller has the <c>authenticated</c> role.</summary>
    bool IsAuthenticated { get; }
}

/// <summary>Default mutable implementation; <c>SwaAuthMiddleware</c> sets <see cref="Principal"/>.</summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private ClientPrincipal _principal = ClientPrincipal.Anonymous;

    public ClientPrincipal Principal
    {
        get => _principal;
        set => _principal = value ?? ClientPrincipal.Anonymous;
    }

    public bool IsAuthenticated => _principal.IsAuthenticated;
}
