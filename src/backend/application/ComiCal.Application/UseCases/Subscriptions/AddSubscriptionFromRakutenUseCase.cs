using ComiCal.Application.Dtos;
using ComiCal.Application.Interfaces;
using ComiCal.Application.Services;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Subscriptions;

/// <summary>
/// 楽天 Books の ISBN を元にシリーズを UPSERT して購読登録するユースケース。
/// DB に未登録のシリーズを検索結果から直接購読する際に使用する。
/// </summary>
public sealed class AddSubscriptionFromRakutenUseCase(
    IRakutenBookSearchService rakutenSearch,
    SeriesUpsertService seriesUpsert,
    ISeriesRepository seriesRepo,
    ISubscriptionRepository subRepo)
{
    public async Task<Result<SubscriptionDto>> ExecuteAsync(
        Guid userId, string isbn13, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(isbn13)
            || isbn13.Length != 13
            || !isbn13.All(char.IsDigit))
        {
            return Result.Failure<SubscriptionDto>(Error.Validation("isbn13 の形式が正しくありません。"));
        }

        // 1. 楽天 Books で ISBN 検索
        var item = await rakutenSearch.SearchByIsbnAsync(isbn13, ct);
        if (item is null)
            return Result.Failure<SubscriptionDto>(Error.NotFound("RakutenBook"));

        // 2. Series / Author / Publisher / Volume を UPSERT
        var seriesId = await seriesUpsert.UpsertAsync(item, ct);

        // 3. 購読登録
        var existing = await subRepo.FindAsync(userId, seriesId, ct);
        if (existing is not null && !existing.IsDeleted)
            return Result.Failure<SubscriptionDto>(Error.AlreadyExists("Subscription"));

        var series = await seriesRepo.FindByIdAsync(seriesId, ct);
        var sub = Subscription.Create(userId, seriesId);
        await subRepo.UpsertAsync(sub, ct);

        return Result.Success(new SubscriptionDto(
            sub.SubscriptionId,
            sub.SeriesId,
            series?.Title ?? item.Title,
            sub.CreatedAt));
    }
}
