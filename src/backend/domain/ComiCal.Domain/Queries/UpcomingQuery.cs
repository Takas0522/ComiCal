namespace ComiCal.Domain.Queries;

public sealed record UpcomingQuery(
    string? Cursor,
    int PageSize = 20,
    IReadOnlyList<Guid>? FilterBySeriesIds = null);
