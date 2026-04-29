using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.Mappings;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.Specifications;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases;

/// <summary>巻検索クエリ。</summary>
public sealed record SearchVolumesQuery(
    string? Query,
    DateOnly? ReleaseFrom,
    DateOnly? ReleaseTo,
    int Limit,
    string? Cursor);

/// <summary>巻検索ユースケース。</summary>
public interface ISearchVolumesUseCase
{
    Task<Result<VolumeSearchResultDto>> ExecuteAsync(
        SearchVolumesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="ISearchVolumesUseCase" />
public sealed class SearchVolumesUseCase(
    IValidator<SearchVolumesQuery> validator,
    IVolumeRepository repository,
    IHiraganaNormalizer hiraganaNormalizer) : ISearchVolumesUseCase
{
    private readonly IValidator<SearchVolumesQuery> _validator = validator;
    private readonly IVolumeRepository _repository = repository;
    private readonly IHiraganaNormalizer _hiraganaNormalizer = hiraganaNormalizer;

    /// <inheritdoc />
    public async Task<Result<VolumeSearchResultDto>> ExecuteAsync(
        SearchVolumesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var validation = await _validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<VolumeSearchResultDto>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        DateOnly? cursorReleaseDate = null;
        Guid? cursorVolumeId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!VolumeCursor.TryDecode(query.Cursor, out var cd, out var cv))
            {
                return Result<VolumeSearchResultDto>.Failure(
                    ApplicationErrors.Validation("Cursor is malformed."));
            }
            cursorReleaseDate = cd;
            cursorVolumeId = cv;
        }

        var normalizedQuery = string.IsNullOrWhiteSpace(query.Query)
            ? null
            : _hiraganaNormalizer.ToHiragana(query.Query.Trim());

        var criteria = new VolumeSearchCriteria(
            Query: normalizedQuery,
            ReleaseFrom: query.ReleaseFrom,
            ReleaseTo: query.ReleaseTo,
            PublisherId: null,
            Limit: query.Limit,
            CursorReleaseDate: cursorReleaseDate,
            CursorVolumeId: cursorVolumeId);

        var volumes = await _repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var items = volumes.Select(v => v.ToDto()).ToList();

        string? nextCursor = null;
        if (items.Count == query.Limit && items.Count > 0)
        {
            var last = volumes[^1];
            if (last.ReleaseDate is { } rd)
            {
                nextCursor = VolumeCursor.Encode(rd, last.Id);
            }
        }

        return Result<VolumeSearchResultDto>.Success(new VolumeSearchResultDto(items, nextCursor));
    }
}
