using ComiCal.Application.Dtos;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Series;

public sealed class GetSeriesDetailUseCase(ISeriesRepository seriesRepo)
{
    public async Task<Result<SeriesDetailDto>> ExecuteAsync(
        Guid seriesId, string? blobBaseUrl, CancellationToken ct = default)
    {
        var series = await seriesRepo.FindByIdAsync(seriesId, ct);
        if (series is null) return Result.Failure<SeriesDetailDto>(Error.NotFound("Series"));
        return Result.Success(SeriesMapper.ToDetailDto(series, blobBaseUrl));
    }
}
