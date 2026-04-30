using ComiCal.Application.Dtos;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Volumes;

public sealed class GetCalendarVolumesUseCase(IVolumeRepository volumeRepo)
{
    public async Task<Result<IReadOnlyDictionary<string, IReadOnlyList<VolumeDto>>>> ExecuteAsync(
        CalendarQuery query, string? blobBaseUrl, CancellationToken ct = default)
    {
        var volumes = await volumeRepo.GetCalendarAsync(query, ct);
        var grouped = volumes
            .Select(v => VolumeMapper.ToDto(v, blobBaseUrl))
            .GroupBy(v => v.ReleaseDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "tbd")
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<VolumeDto>)g.ToList());
        return Result.Success((IReadOnlyDictionary<string, IReadOnlyList<VolumeDto>>)grouped);
    }
}
