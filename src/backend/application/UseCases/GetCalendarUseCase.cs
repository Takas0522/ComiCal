using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Application.Mappings;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases;

/// <summary>カレンダー取得クエリ。<paramref name="MonthFrom"/> から <paramref name="MonthCount"/> ヶ月分。</summary>
public sealed record GetCalendarQuery(DateOnly MonthFrom, int MonthCount = 3);

/// <summary>カレンダー取得ユースケース。</summary>
public interface IGetCalendarUseCase
{
    Task<Result<CalendarDto>> ExecuteAsync(
        GetCalendarQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IGetCalendarUseCase" />
public sealed class GetCalendarUseCase(
    IValidator<GetCalendarQuery> validator,
    IVolumeRepository repository) : IGetCalendarUseCase
{
    private const int CalendarHardLimit = 5_000;

    private readonly IValidator<GetCalendarQuery> _validator = validator;
    private readonly IVolumeRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<CalendarDto>> ExecuteAsync(
        GetCalendarQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var validation = await _validator.ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<CalendarDto>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var monthStart = new DateOnly(query.MonthFrom.Year, query.MonthFrom.Month, 1);
        var endExclusive = monthStart.AddMonths(query.MonthCount);
        var endInclusive = endExclusive.AddDays(-1);

        var volumes = await _repository.GetByReleaseRangeAsync(
            from: monthStart,
            to: endInclusive,
            limit: CalendarHardLimit,
            cursor: null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var days = volumes
            .Where(v => v.ReleaseDate is not null)
            .GroupBy(v => v.ReleaseDate!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new CalendarDayDto(
                Date: g.Key,
                Volumes: g
                    .OrderBy(v => v.VolumeNumber ?? int.MaxValue)
                    .ThenBy(v => v.Id)
                    .Select(v => v.ToDto())
                    .ToList()))
            .ToList();

        return Result<CalendarDto>.Success(new CalendarDto(monthStart, query.MonthCount, days));
    }
}
