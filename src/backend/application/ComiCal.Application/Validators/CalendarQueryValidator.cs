using ComiCal.Domain.Queries;
using FluentValidation;

namespace ComiCal.Application.Validators;

public sealed class CalendarQueryValidator : AbstractValidator<CalendarQuery>
{
    public CalendarQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        When(x => x.Week.HasValue, () =>
            RuleFor(x => x.Week!.Value).InclusiveBetween(1, 53));
    }
}
