using ComiCal.Application.Dtos;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Volumes;

public sealed class GetCalendarVolumesUseCase(IVolumeRepository volumeRepo)
{
    public async Task<Result<CalendarResult>> ExecuteAsync(
        CalendarQuery query, string? blobBaseUrl, CancellationToken ct = default)
    {
        var volumes = await volumeRepo.GetCalendarAsync(query, ct);
        var dtos = volumes.Select(v => VolumeMapper.ToDto(v, blobBaseUrl)).ToList();

        var days = dtos
            .Where(v => v.ReleaseDate.HasValue)
            .GroupBy(v => v.ReleaseDate!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture))
            .OrderBy(g => g.Key)
            .Select(g => new CalendarDayDto(g.Key, (IReadOnlyList<VolumeDto>)g.ToList()))
            .ToList();

        var undated = dtos.Where(v => !v.ReleaseDate.HasValue).ToList();

        return Result.Success(new CalendarResult(days, undated));
    }
}

