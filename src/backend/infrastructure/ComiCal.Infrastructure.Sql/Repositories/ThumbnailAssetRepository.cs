using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ComiCal.Infrastructure.Sql.Repositories;

public sealed class ThumbnailAssetRepository(ComiCalDbContext db) : IThumbnailAssetRepository
{
    public Task<ThumbnailAsset?> FindByVolumeIdAsync(Guid volumeId, CancellationToken ct = default)
        => db.ThumbnailAssets.FindAsync([volumeId], ct).AsTask();

    public async Task<IReadOnlyList<Volume>> GetVolumesWithoutThumbnailAsync(CancellationToken ct = default)
        => await db.Volumes
            .Where(v => !v.IsDeleted && v.ThumbnailAsset == null)
            .ToListAsync(ct);

    public async Task UpsertAsync(ThumbnailAsset asset, CancellationToken ct = default)
    {
        var existing = await db.ThumbnailAssets.FindAsync([asset.VolumeId], ct);
        if (existing is null)
            db.ThumbnailAssets.Add(asset);
        else
            db.Entry(existing).CurrentValues.SetValues(asset);
        await db.SaveChangesAsync(ct);
    }
}
