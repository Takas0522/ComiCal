namespace ComiCal.Application.Dtos;
public record SyncQrUploadDto(string Token, DateTime ExpiresAt);
public record SyncQrDataDto(string EncryptedPayload);
