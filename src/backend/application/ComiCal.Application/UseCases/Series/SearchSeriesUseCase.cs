using ComiCal.Application.Dtos;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Queries;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Series;

public sealed class SearchSeriesUseCase(ISeriesRepository seriesRepo)
{
    public async Task<Result<PagedResult<SeriesDto>>> ExecuteAsync(
        SeriesSearchQuery query, string? blobBaseUrl, CancellationToken ct = default)
    {
        var (items, nextCursor) = await seriesRepo.SearchAsync(query, ct);
        var dtos = items.Select(s => SeriesMapper.ToDto(s, blobBaseUrl)).ToList();
        return Result.Success(new PagedResult<SeriesDto>(dtos, nextCursor));
    }
}
