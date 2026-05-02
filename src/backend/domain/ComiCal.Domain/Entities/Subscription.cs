namespace ComiCal.Domain.Entities;

public sealed class Subscription
{
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SeriesId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Series? Series { get; private set; }

    private Subscription(Guid subscriptionId, Guid userId, Guid seriesId, DateTime createdAt)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        SeriesId = seriesId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Subscription Create(Guid userId, Guid seriesId)
    {
        var now = DateTime.UtcNow;
        return new Subscription(Guid.NewGuid(), userId, seriesId, now);
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
