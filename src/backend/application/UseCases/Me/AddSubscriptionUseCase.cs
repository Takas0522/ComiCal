using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;
using FluentValidation;

namespace ComiCal.Application.UseCases.Me;

/// <summary>購読登録コマンド。</summary>
public sealed record AddSubscriptionCommand(Guid SeriesId);

/// <summary>購読登録ユースケースの結果。<see cref="Created"/> = 新規挿入（HTTP 201）、<c>false</c> = 既存（HTTP 200）。</summary>
public sealed record AddSubscriptionResult(SubscriptionDto Subscription, bool Created);

/// <summary>購読登録ユースケース（冪等 UPSERT）。</summary>
public interface IAddSubscriptionUseCase
{
    Task<Result<AddSubscriptionResult>> ExecuteAsync(
        AddSubscriptionCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IAddSubscriptionUseCase" />
public sealed class AddSubscriptionUseCase(
    IValidator<AddSubscriptionCommand> validator,
    ISubscriptionRepository subscriptions,
    ISeriesRepository series) : IAddSubscriptionUseCase
{
    private readonly IValidator<AddSubscriptionCommand> _validator = validator;
    private readonly ISubscriptionRepository _subscriptions = subscriptions;
    private readonly ISeriesRepository _series = series;

    /// <inheritdoc />
    public async Task<Result<AddSubscriptionResult>> ExecuteAsync(
        AddSubscriptionCommand command,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<AddSubscriptionResult>.Failure(MeErrors.AuthRequired());
        }

        var validation = await _validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return Result<AddSubscriptionResult>.Failure(
                ApplicationErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        // 404 if the target series itself does not exist (cannot subscribe to a phantom).
        var target = await _series.GetByIdAsync(command.SeriesId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Result<AddSubscriptionResult>.Failure(ApplicationErrors.SeriesNotFound(command.SeriesId));
        }

        var (entity, outcome) = await _subscriptions.UpsertAsync(userId, command.SeriesId, cancellationToken).ConfigureAwait(false);
        var dto = new SubscriptionDto(entity.Id, entity.SeriesId, entity.CreatedAt);
        return Result<AddSubscriptionResult>.Success(new AddSubscriptionResult(dto, outcome == UpsertOutcome.Created));
    }
}
