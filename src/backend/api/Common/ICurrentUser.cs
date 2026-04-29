namespace ComiCal.Api.Common;

/// <summary>
/// Scoped accessor exposing the resolved internal <c>Users</c> row for the
/// current Function invocation. Populated by
/// <c>CurrentUserResolverMiddleware</c> after <c>SwaAuthMiddleware</c> has
/// validated the SWA principal. For anonymous requests the accessor reports
/// <see cref="IsAuthenticated"/> = <c>false</c> and <see cref="Id"/> =
/// <see cref="Guid.Empty"/>.
/// </summary>
/// <remarks>
/// Distinct from <see cref="ICurrentUserAccessor"/>: that accessor surfaces
/// the raw <see cref="ClientPrincipal"/> from the SWA header, while this
/// accessor surfaces the persisted domain identity (<c>Users.UserId</c>),
/// resolved via <see cref="ComiCal.Domain.Repositories.IUserRepository"/>.
/// </remarks>
public interface ICurrentUser
{
    /// <summary>Internal <c>Users.UserId</c> (GUID PK). <see cref="Guid.Empty"/> when anonymous.</summary>
    Guid Id { get; }

    /// <summary>IdP subject (SWA <c>userId</c>). Empty when anonymous.</summary>
    string ExternalId { get; }

    /// <summary>Display name pulled from the <c>Users</c> row. Empty when anonymous.</summary>
    string DisplayName { get; }

    /// <summary><c>true</c> iff a <c>Users</c> row has been resolved for this invocation.</summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Default mutable implementation. <c>CurrentUserResolverMiddleware</c> calls
/// <see cref="Populate"/> exactly once per authenticated invocation; anonymous
/// invocations leave the accessor in its default empty state.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public Guid Id { get; private set; }

    /// <inheritdoc />
    public string ExternalId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string DisplayName { get; private set; } = string.Empty;

    /// <inheritdoc />
    public bool IsAuthenticated => Id != Guid.Empty;

    /// <summary>Populates the accessor with a resolved domain user.</summary>
    public void Populate(Guid id, string externalId, string displayName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Resolved user id must not be empty.", nameof(id));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        ArgumentNullException.ThrowIfNull(displayName);

        Id = id;
        ExternalId = externalId;
        DisplayName = displayName;
    }
}
