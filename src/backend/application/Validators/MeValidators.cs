using ComiCal.Application.UseCases.Me;
using FluentValidation;

namespace ComiCal.Application.Validators;

/// <summary><see cref="AddSubscriptionCommand"/> 用バリデータ。</summary>
public sealed class AddSubscriptionCommandValidator : AbstractValidator<AddSubscriptionCommand>
{
    public AddSubscriptionCommandValidator()
    {
        RuleFor(x => x.SeriesId)
            .NotEqual(Guid.Empty)
            .WithMessage("SeriesId must not be empty.");
    }
}

/// <summary><see cref="AddPurchaseCommand"/> 用バリデータ。</summary>
public sealed class AddPurchaseCommandValidator : AbstractValidator<AddPurchaseCommand>
{
    public AddPurchaseCommandValidator()
    {
        RuleFor(x => x.VolumeId)
            .NotEqual(Guid.Empty)
            .WithMessage("VolumeId must not be empty.");
        RuleFor(x => x.PurchasedAt)
            .Must(d => d is null || d.Value <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("PurchasedAt must not be in the future.");
    }
}

/// <summary><see cref="RedeemSyncTokenCommand"/> 用バリデータ。</summary>
public sealed class RedeemSyncTokenCommandValidator : AbstractValidator<RedeemSyncTokenCommand>
{
    public RedeemSyncTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token must not be empty.")
            .MaximumLength(256)
            .WithMessage("Token is unexpectedly long.");
    }
}

/// <summary><see cref="MergeAnonymousDataCommand"/> 用バリデータ。サイズ上限と各項目の妥当性を検査する。</summary>
public sealed class MergeAnonymousDataCommandValidator : AbstractValidator<MergeAnonymousDataCommand>
{
    /// <summary>1 リクエストで取り込める購読の上限。</summary>
    public const int MaxSubscriptions = 1000;

    /// <summary>1 リクエストで取り込める購入の上限。</summary>
    public const int MaxPurchases = 5000;

    public MergeAnonymousDataCommandValidator()
    {
        RuleFor(x => x.Subscriptions)
            .NotNull()
            .Must(list => list!.Count <= MaxSubscriptions)
            .WithMessage($"Too many subscriptions (max {MaxSubscriptions}).");

        RuleFor(x => x.Purchases)
            .NotNull()
            .Must(list => list!.Count <= MaxPurchases)
            .WithMessage($"Too many purchases (max {MaxPurchases}).");

        RuleForEach(x => x.Subscriptions)
            .ChildRules(s => s.RuleFor(i => i.SeriesId).NotEqual(Guid.Empty)
                .WithMessage("SeriesId must not be empty."));

        RuleForEach(x => x.Purchases)
            .ChildRules(p =>
            {
                p.RuleFor(i => i.VolumeId).NotEqual(Guid.Empty)
                    .WithMessage("VolumeId must not be empty.");
                p.RuleFor(i => i.PurchasedAt)
                    .Must(d => d is null || d.Value <= DateTime.UtcNow.AddMinutes(5))
                    .WithMessage("PurchasedAt must not be in the future.");
            });
    }
}
