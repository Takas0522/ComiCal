namespace ComiCal.Domain.Queries;

public sealed record SeriesSearchQuery(
    string? Q,
    DateOnly? ReleaseFrom,
    string? Publisher,
    string? Cursor,
    int PageSize = 20);
