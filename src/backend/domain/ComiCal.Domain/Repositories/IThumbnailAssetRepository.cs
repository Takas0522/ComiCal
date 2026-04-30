using ComiCal.Domain.Entities;

namespace ComiCal.Domain.Repositories;

public interface IThumbnailAssetRepository
{
    Task<ThumbnailAsset?> FindByVolumeIdAsync(Guid volumeId, CancellationToken ct = default);
    Task<IReadOnlyList<Volume>> GetVolumesWithoutThumbnailAsync(CancellationToken ct = default);
    Task UpsertAsync(ThumbnailAsset asset, CancellationToken ct = default);
}
