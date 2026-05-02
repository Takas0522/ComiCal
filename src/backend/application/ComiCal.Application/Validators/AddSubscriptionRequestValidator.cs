using FluentValidation;

namespace ComiCal.Application.Validators;

public record AddSubscriptionRequest(Guid SeriesId);

public sealed class AddSubscriptionRequestValidator : AbstractValidator<AddSubscriptionRequest>
{
    public AddSubscriptionRequestValidator()
    {
        RuleFor(x => x.SeriesId).NotEmpty().WithMessage("SeriesId は必須です。");
    }
}
