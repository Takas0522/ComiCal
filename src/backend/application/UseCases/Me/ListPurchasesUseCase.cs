using ComiCal.Application.Common;
using ComiCal.Application.DTOs;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Me;

/// <summary>現在のユーザーの購入一覧取得クエリ。</summary>
public sealed record ListPurchasesQuery();

/// <summary>現在のユーザーの購入一覧取得ユースケース。</summary>
public interface IListPurchasesUseCase
{
    Task<Result<PurchaseListDto>> ExecuteAsync(
        ListPurchasesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken);
}

/// <inheritdoc cref="IListPurchasesUseCase" />
public sealed class ListPurchasesUseCase(IPurchaseRepository repository) : IListPurchasesUseCase
{
    private readonly IPurchaseRepository _repository = repository;

    /// <inheritdoc />
    public async Task<Result<PurchaseListDto>> ExecuteAsync(
        ListPurchasesQuery query,
        UseCaseContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result<PurchaseListDto>.Failure(MeErrors.AuthRequired());
        }

        var purchases = await _repository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        var items = purchases
            .Select(p => new PurchaseDto(p.Id, p.VolumeId, p.State, p.PurchasedAt, p.CreatedAt))
            .ToList();
        return Result<PurchaseListDto>.Success(new PurchaseListDto(items));
    }
}
