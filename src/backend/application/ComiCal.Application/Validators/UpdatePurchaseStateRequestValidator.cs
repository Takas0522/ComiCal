using FluentValidation;

namespace ComiCal.Application.Validators;

public record UpdatePurchaseStateRequest(string State);

public sealed class UpdatePurchaseStateRequestValidator : AbstractValidator<UpdatePurchaseStateRequest>
{
    private static readonly string[] ValidStates = ["NotPurchased", "Reserved", "Purchased", "Read"];

    public UpdatePurchaseStateRequestValidator()
    {
        RuleFor(x => x.State)
            .NotEmpty()
            .Must(s => ValidStates.Contains(s))
            .WithMessage($"State は {string.Join(", ", ValidStates)} のいずれかである必要があります。");
    }
}
