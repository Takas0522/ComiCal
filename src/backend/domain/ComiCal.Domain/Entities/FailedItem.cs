namespace ComiCal.Domain.Entities;

public sealed class FailedItem
{
    public Guid FailedItemId { get; private set; }
    public Guid BatchRunId { get; private set; }
    public string ItemKey { get; private set; }
    public string Reason { get; private set; }
    public string? PayloadJson { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FailedItem(Guid failedItemId, Guid batchRunId, string itemKey, string reason, string? payloadJson, DateTime createdAt)
    {
        FailedItemId = failedItemId;
        BatchRunId = batchRunId;
        ItemKey = itemKey;
        Reason = reason;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
    }

    public static FailedItem Create(Guid batchRunId, string itemKey, string reason, string? payloadJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new FailedItem(
            Guid.NewGuid(),
            batchRunId,
            itemKey[..Math.Min(itemKey.Length, 256)],
            reason[..Math.Min(reason.Length, 1024)],
            payloadJson is null ? null : payloadJson[..Math.Min(payloadJson.Length, 4000)],
            DateTime.UtcNow);
    }
}
