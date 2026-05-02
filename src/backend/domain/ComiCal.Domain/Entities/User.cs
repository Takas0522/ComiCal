using ComiCal.Domain.Enums;

namespace ComiCal.Domain.Entities;

public sealed class User
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? AgreedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<IdentityLink> _identityLinks = [];
    public IReadOnlyCollection<IdentityLink> IdentityLinks => _identityLinks.AsReadOnly();

#pragma warning disable CS8618
    // For EF Core materialization
    private User() { }
#pragma warning restore CS8618

    private User(Guid userId, string displayName, UserRole role, DateTime? agreedAt, DateTime createdAt)
    {
        UserId = userId;
        DisplayName = displayName;
        Role = role;
        AgreedAt = agreedAt;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static User Create(string displayName, DateTime? agreedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var now = DateTime.UtcNow;
        return new User(Guid.NewGuid(), displayName[..Math.Min(displayName.Length, 64)], UserRole.User, agreedAt, now);
    }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName[..Math.Min(displayName.Length, 64)];
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddIdentityLink(IdentityLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        _identityLinks.Add(link);
    }
}
