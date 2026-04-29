using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary>現在のユーザーの購読一覧取得クエリ。</summary>
public sealed record ListSubscriptionsQuery();

/// <summary>現在のユーザーの購読一覧取得ユースケース。</summary>
public interface IListSubscriptionsUseCase
{
    Task<Result<SubscriptionListDto>> ExecuteAsync(
        ListSubscriptionsQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IListSubscriptionsUseCase" />
public sealed class ListSubscriptionsUseCase(ISubscriptionRepository repository) : IListSubscriptionsUseCase
{
    private readonly ISubscriptionRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<SubscriptionListDto>> ExecuteAsync(
        ListSubscriptionsQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<SubscriptionListDto>.Failure(MeErrors.AuthRequired());
        }

        var subs = await _repository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var items = subs
            .Select(s => new SubscriptionDto(s.Id, s.SeriesId, s.CreatedAt))
            .ToList();
        return Result<SubscriptionListDto>.Success(new SubscriptionListDto(items));
    }
}
