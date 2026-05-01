namespace ComiCal.Application.Dtos;

public record SubscriptionDto(Guid SubscriptionId, Guid SeriesId, string SeriesTitle, DateTime CreatedAt);
