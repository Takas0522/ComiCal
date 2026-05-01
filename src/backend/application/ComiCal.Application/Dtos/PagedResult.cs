namespace ComiCal.Application.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor);
