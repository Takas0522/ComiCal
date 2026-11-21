using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Jobs;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Jobs;

public sealed class FetchPagesJobTests
{
    [Fact]
    public async Task RunAsync_MultiplePages_UpsertsEachPageInOrderAndReturnsSummary()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new FetchPagesJob(invoker, NullLogger<FetchPagesJob>.Instance);
        var runId = Guid.NewGuid();
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);
        var ct = CancellationToken.None;
        RakutenVolumeData[] firstItems = [new("9784088726236", "First", "", "", "", null, null)];
        RakutenVolumeData[] secondItems = [new("9784088726243", "Second", "", "", "", null, null)];
        var firstThumbnail = new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/1.jpg", null);
        var secondThumbnail = new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/2.jpg", null);

        Fetch(1).Returns(new FetchPageOutput(2, 5, firstItems));
        Fetch(2).Returns(new FetchPageOutput(2, 7, secondItems));
        Upsert(firstItems).Returns(new UpsertVolumesOutput(3, [firstThumbnail], ["first-failure"]));
        Upsert(secondItems).Returns(new UpsertVolumesOutput(4, [secondThumbnail], ["last-failure-1", "last-failure-2"]));

        var result = await job.RunAsync(runId, from, to, ct);

        Received.InOrder(() =>
        {
            _ = Fetch(1);
            _ = Upsert(firstItems);
            _ = Fetch(2);
            _ = Upsert(secondItems);
        });
        Assert.Equal(4, invoker.ReceivedCalls().Count());
        Assert.Equal(12, result.FetchedCount);
        Assert.Equal(7, result.UpsertedCount);
        Assert.Equal(3, result.FailedCount);
        Assert.Equal([firstThumbnail, secondThumbnail], result.ThumbnailPending);

        Task<FetchPageOutput> Fetch(int page)
            => invoker.RunAsync<FetchPageActivity, FetchPageInput, FetchPageOutput>(
                new(runId, page, from, to), Arg.Any<Func<FetchPageActivity, FetchPageInput, CancellationToken, Task<FetchPageOutput>>>(), ct);

        Task<UpsertVolumesOutput> Upsert(IReadOnlyList<RakutenVolumeData> items)
            => invoker.RunAsync<UpsertVolumesActivity, UpsertVolumesInput, UpsertVolumesOutput>(
                new(runId, items), Arg.Any<Func<UpsertVolumesActivity, UpsertVolumesInput, CancellationToken, Task<UpsertVolumesOutput>>>(), ct);
    }
}
