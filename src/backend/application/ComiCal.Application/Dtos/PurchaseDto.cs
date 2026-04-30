namespace ComiCal.Application.Dtos;
public record PurchaseDto(Guid PurchaseId, Guid VolumeId, string PurchaseState, DateTime UpdatedAt);
