using ComiCal.Batch.Models;
using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Blob;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

[DurableTask("DownloadThumbnailActivity")]
public partial class DownloadThumbnailActivity(
    BlobStorageService blobService,
    IThumbnailAssetRepository thumbnailRepo,
    ILogger<DownloadThumbnailActivity> logger)
    : TaskActivity<DownloadThumbnailInput, DownloadThumbnailOutput>
{
    public override async Task<DownloadThumbnailOutput> RunAsync(
        TaskActivityContext context, DownloadThumbnailInput input)
    {
        try
        {
            var asset = await blobService.UploadThumbnailIfChangedAsync(
                input.VolumeId, input.ImageUrl, input.ExistingHash);

            if (asset is null)
            {
                return new DownloadThumbnailOutput(false, true, false, null);
            }

            await thumbnailRepo.UpsertAsync(asset);
            LogThumbnailDownloaded(logger, input.VolumeId);
            return new DownloadThumbnailOutput(true, false, false, null);
        }
        catch (Exception ex)
        {
            LogThumbnailFailed(logger, input.VolumeId, ex);
            return new DownloadThumbnailOutput(false, false, true, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Thumbnail downloaded for volume {VolumeId}")]
    private static partial void LogThumbnailDownloaded(ILogger logger, Guid volumeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to download thumbnail for volume {VolumeId}")]
    private static partial void LogThumbnailFailed(ILogger logger, Guid volumeId, Exception ex);
}
