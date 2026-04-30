using ComiCal.Application.Dtos;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases.Subscriptions;

public sealed class AddSubscriptionUseCase(
    ISubscriptionRepository subRepo,
    ISeriesRepository seriesRepo)
{
    public async Task<Result<SubscriptionDto>> ExecuteAsync(
        Guid userId, Guid seriesId, CancellationToken ct = default)
    {
        var series = await seriesRepo.FindByIdAsync(seriesId, ct);
        if (series is null) return Result.Failure<SubscriptionDto>(Error.NotFound("Series"));

        var existing = await subRepo.FindAsync(userId, seriesId, ct);
        if (existing is not null && !existing.IsDeleted)
            return Result.Failure<SubscriptionDto>(Error.AlreadyExists("Subscription"));

        var sub = Subscription.Create(userId, seriesId);
        await subRepo.UpsertAsync(sub, ct);

        return Result.Success(new SubscriptionDto(sub.SubscriptionId, sub.SeriesId, series.Title, sub.CreatedAt));
    }
}
