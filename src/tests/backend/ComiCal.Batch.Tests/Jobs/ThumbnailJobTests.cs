using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Jobs;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Jobs;

public sealed class ThumbnailJobTests
{
    [Fact]
    public async Task RunAsync_MixedItems_ForwardsEachInputAndAggregatesResults()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new ThumbnailJob(invoker, NullLogger<ThumbnailJob>.Instance);
        var runId = Guid.NewGuid();
        ThumbnailPendingItem[] items =
        [
            new(Guid.NewGuid(), "https://img.example.com/1.jpg", [1]),
            new(Guid.NewGuid(), "https://img.example.com/2.jpg", [2]),
            new(Guid.NewGuid(), "https://img.example.com/3.jpg", null)
        ];
        Download(invoker, new(runId, items[0].VolumeId, items[0].ImageUrl, items[0].ExistingHash))
            .Returns(new DownloadThumbnailOutput(true, false, false, null));
        Download(invoker, new(runId, items[1].VolumeId, items[1].ImageUrl, items[1].ExistingHash))
            .Returns(new DownloadThumbnailOutput(false, true, false, null));
        Download(invoker, new(runId, items[2].VolumeId, items[2].ImageUrl, items[2].ExistingHash))
            .Returns(new DownloadThumbnailOutput(false, false, true, "error"));

        var summary = await job.RunAsync(runId, items, CancellationToken.None);

        Assert.Equal(new ThumbnailSummary(1, 1, 1), summary);
        foreach (var item in items)
            await Download(invoker.Received(1), new(runId, item.VolumeId, item.ImageUrl, item.ExistingHash));
        Assert.Equal(3, invoker.ReceivedCalls().Count());
    }

    [Fact]
    public async Task RunAsync_ExhaustedTimeout_CountsItemAsFailedAndContinues()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new ThumbnailJob(invoker, NullLogger<ThumbnailJob>.Instance);
        var runId = Guid.NewGuid();
        ThumbnailPendingItem[] items =
        [
            new(Guid.NewGuid(), "https://img.example.com/slow.jpg", null),
            new(Guid.NewGuid(), "https://img.example.com/ok.jpg", null)
        ];
        Download(invoker, new(runId, items[0].VolumeId, items[0].ImageUrl, items[0].ExistingHash))
            .Returns(Task.FromException<DownloadThumbnailOutput>(new TaskCanceledException("HttpClient timeout")));
        Download(invoker, new(runId, items[1].VolumeId, items[1].ImageUrl, items[1].ExistingHash))
            .Returns(new DownloadThumbnailOutput(true, false, false, null));

        var summary = await job.RunAsync(runId, items, CancellationToken.None);

        Assert.Equal(new ThumbnailSummary(1, 0, 1), summary);
    }

    [Fact]
    public async Task RunAsync_CallerCancellation_Propagates()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new ThumbnailJob(invoker, NullLogger<ThumbnailJob>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.RunAsync(Guid.NewGuid(),
            [new(Guid.NewGuid(), "https://img.example.com/1.jpg", null)], cts.Token));
    }

    [Fact]
    public async Task RunAsync_EmptyItems_ReturnsZeroCounts()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new ThumbnailJob(invoker, NullLogger<ThumbnailJob>.Instance);

        var summary = await job.RunAsync(Guid.NewGuid(), [], CancellationToken.None);

        Assert.Equal(new ThumbnailSummary(0, 0, 0), summary);
        Assert.Empty(invoker.ReceivedCalls());
    }

    [Fact]
    public async Task RunAsync_NineItems_RunsAtMostEightConcurrently()
    {
        var invoker = Substitute.For<IActivityInvoker>();
        var job = new ThumbnailJob(invoker, NullLogger<ThumbnailJob>.Instance);
        var items = Enumerable.Range(0, 9)
            .Select(_ => new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/1.jpg", null)).ToArray();
        var eightStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        Download(invoker, Arg.Any<DownloadThumbnailInput>()).Returns(async _ =>
        {
            if (Interlocked.Increment(ref started) == 8) eightStarted.SetResult();
            await release.Task;
            return new DownloadThumbnailOutput(true, false, false, null);
        });

        var run = job.RunAsync(Guid.NewGuid(), items, CancellationToken.None);
        try
        {
            await eightStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(8, Volatile.Read(ref started));
        }
        finally
        {
            release.SetResult();
        }

        Assert.Equal(new ThumbnailSummary(9, 0, 0), await run);
    }

    private static Task<DownloadThumbnailOutput> Download(IActivityInvoker invoker, DownloadThumbnailInput input)
        => invoker.RunAsync<DownloadThumbnailActivity, DownloadThumbnailInput, DownloadThumbnailOutput>(
            input, Arg.Any<Func<DownloadThumbnailActivity, DownloadThumbnailInput, CancellationToken, Task<DownloadThumbnailOutput>>>(),
            Arg.Any<CancellationToken>(), thumbnail: true);
}
