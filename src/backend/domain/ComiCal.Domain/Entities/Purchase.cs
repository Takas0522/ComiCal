using ComiCal.Domain.Enums;

namespace ComiCal.Domain.Entities;

public sealed class Purchase
{
    public Guid PurchaseId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid VolumeId { get; private set; }
    public PurchaseState State { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Volume? Volume { get; private set; }

    private Purchase(Guid purchaseId, Guid userId, Guid volumeId, DateTime createdAt)
    {
        PurchaseId = purchaseId;
        UserId = userId;
        VolumeId = volumeId;
        State = PurchaseState.NotPurchased;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Purchase Create(Guid userId, Guid volumeId)
    {
        var now = DateTime.UtcNow;
        return new Purchase(Guid.NewGuid(), userId, volumeId, now);
    }

    public void UpdateState(PurchaseState state)
    {
        State = state;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
