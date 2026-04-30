using ComiCal.Domain.Entities;

namespace ComiCal.Domain.DomainServices;

public sealed record MergeResult(
    IReadOnlyList<Guid> ToAdd,
    IReadOnlyList<Guid> ToKeep,
    IReadOnlyList<Guid> CloudOnly);

public static class MergeStrategy
{
    public static MergeResult MergeSubscriptions(
        IEnumerable<Guid> localSeriesIds,
        IEnumerable<Subscription> cloudSubscriptions)
    {
        ArgumentNullException.ThrowIfNull(localSeriesIds);
        ArgumentNullException.ThrowIfNull(cloudSubscriptions);

        var localSet = localSeriesIds.ToHashSet();
        var cloudSet = cloudSubscriptions
            .Where(s => !s.IsDeleted)
            .Select(s => s.SeriesId)
            .ToHashSet();

        var toAdd = localSet.Except(cloudSet).ToList();
        var toKeep = localSet.Intersect(cloudSet).ToList();
        var cloudOnly = cloudSet.Except(localSet).ToList();

        return new MergeResult(toAdd, toKeep, cloudOnly);
    }
}
