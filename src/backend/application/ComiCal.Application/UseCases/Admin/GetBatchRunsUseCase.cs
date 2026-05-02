using ComiCal.Application.Dtos;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Admin;

public sealed class GetBatchRunsUseCase(IBatchRunRepository batchRunRepo)
{
    public async Task<Result<PagedResult<BatchRunDto>>> ExecuteAsync(
        string? cursor, int pageSize = 20, CancellationToken ct = default)
    {
        var (items, nextCursor) = await batchRunRepo.GetAllAsync(cursor, pageSize, ct);
        var dtos = items.Select(b => new BatchRunDto(
            b.BatchRunId, b.StartedAt, b.CompletedAt,
            b.Status.ToString(),
            b.FetchedItemCount, b.UpsertedVolumeCount,
            b.DownloadedThumbnailCount, b.SkippedThumbnailCount,
            b.FailedItemCount)).ToList();
        return Result.Success(new PagedResult<BatchRunDto>(dtos, nextCursor));
    }
}
