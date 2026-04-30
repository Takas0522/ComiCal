namespace ComiCal.Domain.Queries;

public sealed record CalendarQuery(
    int Year,
    int Month,
    int? Week = null,
    IReadOnlyList<Guid>? FilterBySeriesIds = null);
