using ComiCal.Domain.Queries;
using FluentValidation;

namespace ComiCal.Application.Validators;

public sealed class SearchSeriesRequestValidator : AbstractValidator<SeriesSearchQuery>
{
    public SearchSeriesRequestValidator()
    {
        When(x => x.Q is not null, () =>
            RuleFor(x => x.Q).MaximumLength(100).WithMessage("検索キーワードは100文字以内で入力してください。"));
        When(x => x.Publisher is not null, () =>
            RuleFor(x => x.Publisher).MaximumLength(100));
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
