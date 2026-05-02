using ComiCal.Application.Dtos;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Volumes;

public sealed class GetUpcomingVolumesUseCase(IVolumeRepository volumeRepo)
{
    public async Task<Result<PagedResult<VolumeDto>>> ExecuteAsync(
        UpcomingQuery query, string? blobBaseUrl, CancellationToken ct = default)
    {
        var (items, nextCursor) = await volumeRepo.GetUpcomingAsync(query, ct);
        var dtos = items.Select(v => VolumeMapper.ToDto(v, blobBaseUrl)).ToList();
        return Result.Success(new PagedResult<VolumeDto>(dtos, nextCursor));
    }
}
