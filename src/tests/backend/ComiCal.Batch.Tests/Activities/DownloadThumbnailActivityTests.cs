using Azure.Storage.Blobs;
using ComiCal.Batch.Activities;
using ComiCal.Batch.Models;
using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Blob;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Activities;

public sealed class DownloadThumbnailActivityTests
{
    // BlobStorageService creates its own HttpClient and calls BlobServiceClient internally.
    // Full success/skip paths require a running Azurite container and are covered by integration
    // tests.  Here we exercise only the catch-all exception path, which is triggered by
    // supplying an unsupported URI scheme that HttpClient rejects without making any network I/O.

    [Fact]
    public async Task RunAsync_HttpClientThrows_ReturnsFailed()
    {
        // Arrange — "xyz://" is not a registered HttpClient scheme; GetAsync throws synchronously
        var blobService = new BlobStorageService(new BlobServiceClient("UseDevelopmentStorage=true"));
        var thumbnailRepo = Substitute.For<IThumbnailAssetRepository>();
        var sut = new DownloadThumbnailActivity(
            blobService,
            thumbnailRepo,
            Substitute.For<ILogger<DownloadThumbnailActivity>>());

        var input = new DownloadThumbnailInput(
            Guid.NewGuid(), Guid.NewGuid(),
            "xyz://not-a-valid-httpclient-scheme",
            null);

        // Act
        var result = await sut.Run(input);

        // Assert
        Assert.False(result.Downloaded);
        Assert.False(result.Skipped);
        Assert.True(result.Failed);
        Assert.NotNull(result.FailureReason);
        // Thumbnail upsert must NOT be called when the activity fails
        await thumbnailRepo.DidNotReceive().UpsertAsync(Arg.Any<Domain.Entities.ThumbnailAsset>(), Arg.Any<CancellationToken>());
    }
}
