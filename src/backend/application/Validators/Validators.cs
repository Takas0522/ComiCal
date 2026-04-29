using ComiCal.Application.UseCases;
using FluentValidation;

namespace ComiCal.Application.Validators;

/// <summary><see cref="SearchSeriesQuery"/> 用バリデータ。</summary>
public sealed class SearchSeriesQueryValidator : AbstractValidator<SearchSeriesQuery>
{
    public SearchSeriesQueryValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(256)
            .When(x => x.Query is not null);
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100.");
    }
}

/// <summary><see cref="GetSeriesDetailQuery"/> 用バリデータ。</summary>
public sealed class GetSeriesDetailQueryValidator : AbstractValidator<GetSeriesDetailQuery>
{
    public GetSeriesDetailQueryValidator()
    {
        RuleFor(x => x.SeriesId)
            .NotEqual(Guid.Empty)
            .WithMessage("SeriesId must not be empty.");
    }
}

/// <summary><see cref="SearchVolumesQuery"/> 用バリデータ。</summary>
public sealed class SearchVolumesQueryValidator : AbstractValidator<SearchVolumesQuery>
{
    public SearchVolumesQueryValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(256)
            .When(x => x.Query is not null);
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100)
            .WithMessage("Limit must be between 1 and 100.");
        RuleFor(x => x)
            .Must(x => x.ReleaseFrom is null || x.ReleaseTo is null || x.ReleaseFrom <= x.ReleaseTo)
            .WithMessage("ReleaseFrom must be on or before ReleaseTo.");
    }
}

/// <summary><see cref="GetCalendarQuery"/> 用バリデータ。</summary>
public sealed class GetCalendarQueryValidator : AbstractValidator<GetCalendarQuery>
{
    public GetCalendarQueryValidator()
    {
        RuleFor(x => x.MonthCount)
            .InclusiveBetween(1, 12)
            .WithMessage("MonthCount must be between 1 and 12.");
        RuleFor(x => x.MonthFrom)
            .NotEqual(default(DateOnly))
            .WithMessage("MonthFrom must be specified.");
    }
}

/// <summary><see cref="GetVolumeByIsbnQuery"/> 用バリデータ（書式チェックのみ。チェックディジット検証は UseCase 内）。</summary>
public sealed class GetVolumeByIsbnQueryValidator : AbstractValidator<GetVolumeByIsbnQuery>
{
    public GetVolumeByIsbnQueryValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty()
            .WithMessage("Isbn is required.");
    }
}

/// <summary><see cref="GetHealthQuery"/> 用バリデータ（互換のため空のまま保持）。</summary>
public sealed class GetHealthQueryValidator : AbstractValidator<GetHealthQuery>
{
}
