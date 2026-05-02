using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class PurchaseRepository(ComiCalDbContext db) : IPurchaseRepository
{
    public async Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Purchases
            .Include(p => p.Volume)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .ToListAsync(ct);

    public Task<Purchase?> FindAsync(Guid userId, Guid volumeId, CancellationToken ct = default)
        => db.Purchases.FirstOrDefaultAsync(p => p.UserId == userId && p.VolumeId == volumeId, ct);

    public async Task<Guid> UpsertAsync(Purchase purchase, CancellationToken ct = default)
    {
        var existing = await db.Purchases.FindAsync([purchase.PurchaseId], ct);
        if (existing is null)
            db.Purchases.Add(purchase);
        else
            db.Entry(existing).CurrentValues.SetValues(purchase);
        await db.SaveChangesAsync(ct);
        return purchase.PurchaseId;
    }
}
