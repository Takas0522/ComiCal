using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.Mappings;
using ComiCal.Domain.DomainServices;
using ComiCal.Domain.Repositories;
using ComiCal.Domain.Specifications;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases;

/// <summary>シリーズ検索クエリ。</summary>
public sealed record SearchSeriesQuery(
    string? Query,
    Guid? PublisherId,
    Guid? AuthorId,
    int Limit,
    Guid? Cursor);

/// <summary>シリーズ検索ユースケース。</summary>
public interface ISearchSeriesUseCase
{
    Task<Result<SeriesSearchResultDto>> ExecuteAsync(
        SearchSeriesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="ISearchSeriesUseCase" />
public sealed class SearchSeriesUseCase(
    IValidator<SearchSeriesQuery> validator,
    ISeriesRepository repository,
    IHiraganaNormalizer hiraganaNormalizer) : ISearchSeriesUseCase
{
    private readonly IValidator<SearchSeriesQuery> _validator = validator;
    private readonly ISeriesRepository _repository = repository;
    private readonly IHiraganaNormalizer _hiraganaNormalizer = hiraganaNormalizer;

    /// <inheritdoc />
    public async Task<Result<SeriesSearchResultDto>> ExecuteAsync(
        SearchSeriesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var validation = await _validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<SeriesSearchResultDto>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var normalizedQuery = string.IsNullOrWhiteSpace(query.Query)
            ? null
            : _hiraganaNormalizer.ToHiragana(query.Query.Trim());

        var criteria = new SeriesSearchCriteria(
            Query: normalizedQuery,
            PublisherId: query.PublisherId,
            AuthorId: query.AuthorId,
            Limit: query.Limit,
            CursorSeriesId: query.Cursor);

        var series = await _repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var items = series.Select(s => s.ToSummaryDto()).ToList();
        string? nextCursor = items.Count == query.Limit && items.Count > 0
            ? items[^1].Id.ToString("D")
            : null;

        return Result<SeriesSearchResultDto>.Success(new SeriesSearchResultDto(items, nextCursor));
    }
}
