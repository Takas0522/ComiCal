using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases;

/// <summary>シリーズ詳細取得クエリ。</summary>
public sealed record GetSeriesDetailQuery(Guid SeriesId, DateOnly? ReleaseFrom);

/// <summary>シリーズ詳細取得ユースケース。</summary>
public interface IGetSeriesDetailUseCase
{
    Task<Result<SeriesDetailDto>> ExecuteAsync(
        GetSeriesDetailQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IGetSeriesDetailUseCase" />
public sealed class GetSeriesDetailUseCase(
    IValidator<GetSeriesDetailQuery> validator,
    ISeriesRepository repository) : IGetSeriesDetailUseCase
{
    private readonly IValidator<GetSeriesDetailQuery> _validator = validator;
    private readonly ISeriesRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SeriesDetailDto>> ExecuteAsync(
        GetSeriesDetailQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var validation = await _validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<SeriesDetailDto>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var series = await _repository.GetWithVolumesAsync(query.SeriesId, query.ReleaseFrom, cancellationToken).ConfigureAwait(false);
        if (series is null)
        {
            return Result<SeriesDetailDto>.Failure(ApplicationErrors.SeriesNotFound(query.SeriesId));
        }

        var volumes = series.Volumes
            .OrderBy(v => v.ReleaseDate ?? DateOnly.MaxValue)
            .ThenBy(v => v.VolumeNumber ?? int.MaxValue)
            .Select(v => v.ToDto())
            .ToList();

        return Result<SeriesDetailDto>.Success(new SeriesDetailDto(series.ToSummaryDto(), volumes));
    }
}
