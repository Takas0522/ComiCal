using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ISubscriptionRepository"/>.</summary>
public sealed class SubscriptionRepository(
    ComiCalDbContext db,
    ILogger<SubscriptionRepository> logger) : ISubscriptionRepository
{
    private readonly ComiCalDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ILogger<SubscriptionRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Array.Empty<Subscription>();
        }

        return await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Subscription?> FindAnyAsync(Guid userId, Guid seriesId, CancellationToken cancellationToken)
    {
        return await _db.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeriesId == seriesId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(Subscription Entity, UpsertOutcome Outcome)> UpsertAsync(
        Guid userId,
        Guid seriesId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeriesId == seriesId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
            {
                return (existing, UpsertOutcome.Existing);
            }

            existing.Restore();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return (existing, UpsertOutcome.Created);
        }

        var created = Subscription.CreateNew(Guid.CreateVersion7(), userId, seriesId);
        _db.Subscriptions.Add(created);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex,
                "Concurrent insert detected for subscription (UserId={UserId}, SeriesId={SeriesId}); re-reading existing row.",
                userId, seriesId);
            _db.Entry(created).State = EntityState.Detached;
            var raced = await _db.Subscriptions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SeriesId == seriesId, cancellationToken)
                .ConfigureAwait(false);
            if (raced is null)
            {
                throw;
            }
            return (raced, UpsertOutcome.Existing);
        }

        return (created, UpsertOutcome.Created);
    }

    /// <inheritdoc />
    public async Task<bool> SoftDeleteAsync(Guid userId, Guid seriesId, CancellationToken cancellationToken)
    {
        var entity = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SeriesId == seriesId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }

        entity.SoftDelete(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
