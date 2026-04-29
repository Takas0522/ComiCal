using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IPurchaseRepository"/>.</summary>
public sealed class PurchaseRepository(
    ComiCalDbContext db,
    ILogger<PurchaseRepository> logger) : IPurchaseRepository
{
    private readonly ComiCalDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly ILogger<PurchaseRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Array.Empty<Purchase>();
        }

        return await _db.Purchases
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Purchase?> FindAnyAsync(Guid userId, Guid volumeId, CancellationToken cancellationToken)
    {
        return await _db.Purchases
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.VolumeId == volumeId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(Purchase Entity, UpsertOutcome Outcome)> UpsertAsync(
        Guid userId,
        Guid volumeId,
        DateTime? purchasedAt,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Purchases
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.VolumeId == volumeId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var wasDeleted = existing.IsDeleted;
            existing.UpdatePurchase(purchasedAt);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return (existing, wasDeleted ? UpsertOutcome.Created : UpsertOutcome.Existing);
        }

        var created = Purchase.CreateNew(Guid.CreateVersion7(), userId, volumeId, purchasedAt);
        _db.Purchases.Add(created);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex,
                "Concurrent insert detected for purchase (UserId={UserId}, VolumeId={VolumeId}); re-reading existing row.",
                userId, volumeId);
            _db.Entry(created).State = EntityState.Detached;
            var raced = await _db.Purchases
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.VolumeId == volumeId, cancellationToken)
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
    public async Task<bool> SoftDeleteAsync(Guid userId, Guid volumeId, CancellationToken cancellationToken)
    {
        var entity = await _db.Purchases
            .FirstOrDefaultAsync(p => p.UserId == userId && p.VolumeId == volumeId, cancellationToken)
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
