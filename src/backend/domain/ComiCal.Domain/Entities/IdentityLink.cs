using ComiCal.Domain.Enums;

namespace ComiCal.Domain.Entities;

public sealed class IdentityLink
{
    public Guid IdentityLinkId { get; private set; }
    public Guid UserId { get; private set; }
    public IdentityProvider Provider { get; private set; }
    public string Subject { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private IdentityLink(Guid identityLinkId, Guid userId, IdentityProvider provider, string subject, DateTime createdAt)
    {
        IdentityLinkId = identityLinkId;
        UserId = userId;
        Provider = provider;
        Subject = subject;
        CreatedAt = createdAt;
    }

    public static IdentityLink Create(Guid userId, IdentityProvider provider, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return new IdentityLink(Guid.NewGuid(), userId, provider, subject[..Math.Min(subject.Length, 256)], DateTime.UtcNow);
    }
}
