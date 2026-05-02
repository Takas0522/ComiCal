using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class SubscriptionRepository(ComiCalDbContext db) : ISubscriptionRepository
{
    public async Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Subscriptions
            .Include(s => s.Series)
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public Task<Subscription?> FindAsync(Guid userId, Guid seriesId, CancellationToken ct = default)
        => db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeriesId == seriesId, ct);

    public async Task<Guid> UpsertAsync(Subscription subscription, CancellationToken ct = default)
    {
        var existing = await db.Subscriptions.FindAsync([subscription.SubscriptionId], ct);
        if (existing is null)
            db.Subscriptions.Add(subscription);
        else
            db.Entry(existing).CurrentValues.SetValues(subscription);
        await db.SaveChangesAsync(ct);
        return subscription.SubscriptionId;
    }

    public async Task SoftDeleteAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        var sub = await db.Subscriptions.FindAsync([subscriptionId], ct);
        if (sub is null) return;
        sub.SoftDelete();
        await db.SaveChangesAsync(ct);
    }
}
