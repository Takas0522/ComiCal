using ComiCal.Batch.Activities;
using ComiCal.Batch.Hosting;
using ComiCal.Batch.Jobs;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Jobs;

public sealed class DailyFetchJobTests
{
    private readonly IActivityInvoker _invoker = Substitute.For<IActivityInvoker>();
    private readonly Guid _runId = Guid.NewGuid();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_Success_FinalizesCombinedCountsInOrder(bool hasThumbnail)
    {
        var ct = CancellationToken.None;
        var job = CreateJob(ct);
        var thumbnail = new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/1.jpg", null);
        Fetch(ct).Returns(new FetchPageOutput(1, 10, []));
        Upsert(ct).Returns(new UpsertVolumesOutput(8, hasThumbnail ? [thumbnail] : [], ["failed-isbn"]));
        Download(thumbnail).Returns(new DownloadThumbnailOutput(false, false, true, "error"));

        var result = await job.RunAsync(ct);

        Assert.Equal(_runId, result);
        Received.InOrder(() =>
        {
            _ = Create(ct);
            _ = Fetch(ct);
            _ = Upsert(ct);
            if (hasThumbnail) _ = Download(thumbnail);
            _ = Finalize(new(_runId, 10, 8, 0, 0, hasThumbnail ? 2 : 1, true), ct);
        });
        Assert.Equal(hasThumbnail ? 5 : 4, _invoker.ReceivedCalls().Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunAsync_FetchFailsOrCancels_FinalizesWithoutCancellationAndPropagates(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var job = CreateJob(ct);
        Exception failure = cancel ? new OperationCanceledException(ct) : new InvalidOperationException();
        Fetch(ct).Returns(_ =>
        {
            if (cancel) cancellation.Cancel();
            return Task.FromException<FetchPageOutput>(failure);
        });

        Assert.Same(failure, await Record.ExceptionAsync(() => job.RunAsync(ct)));

        Received.InOrder(() =>
        {
            _ = Create(ct);
            _ = Fetch(ct);
            _ = Finalize(new(_runId, 0, 0, 0, 0, 1, false), CancellationToken.None);
        });
        Assert.Equal(3, _invoker.ReceivedCalls().Count());
    }

    private DailyFetchJob CreateJob(CancellationToken ct)
    {
        Create(ct).Returns(_runId);
        return new DailyFetchJob(_invoker, new FetchPagesJob(_invoker, NullLogger<FetchPagesJob>.Instance),
            new ThumbnailJob(_invoker, NullLogger<ThumbnailJob>.Instance), new FixedClock(), NullLogger<DailyFetchJob>.Instance);
    }

    private Task<Guid> Create(CancellationToken ct)
        => _invoker.RunAsync<CreateBatchRunActivity, object?, Guid>(
            Arg.Is<object?>(input => input == null), Arg.Any<Func<CreateBatchRunActivity, object?, CancellationToken, Task<Guid>>>(), ct);

    private Task<FetchPageOutput> Fetch(CancellationToken ct)
        => _invoker.RunAsync<FetchPageActivity, FetchPageInput, FetchPageOutput>(
            new(_runId, 1, new DateOnly(2024, 12, 15), new DateOnly(2025, 12, 15)),
            Arg.Any<Func<FetchPageActivity, FetchPageInput, CancellationToken, Task<FetchPageOutput>>>(), ct);

    private Task<UpsertVolumesOutput> Upsert(CancellationToken ct)
        => _invoker.RunAsync<UpsertVolumesActivity, UpsertVolumesInput, UpsertVolumesOutput>(
            Arg.Is<UpsertVolumesInput>(input => input.BatchRunId == _runId && input.Items.Count == 0),
            Arg.Any<Func<UpsertVolumesActivity, UpsertVolumesInput, CancellationToken, Task<UpsertVolumesOutput>>>(), ct);

    private Task<DownloadThumbnailOutput> Download(ThumbnailPendingItem item)
        => _invoker.RunAsync<DownloadThumbnailActivity, DownloadThumbnailInput, DownloadThumbnailOutput>(
            new(_runId, item.VolumeId, item.ImageUrl, item.ExistingHash),
            Arg.Any<Func<DownloadThumbnailActivity, DownloadThumbnailInput, CancellationToken, Task<DownloadThumbnailOutput>>>(),
            Arg.Any<CancellationToken>(), thumbnail: true);

    private Task<bool> Finalize(FinalizeBatchRunInput input, CancellationToken ct)
        => _invoker.RunAsync<FinalizeBatchRunActivity, FinalizeBatchRunInput, bool>(
            input, Arg.Any<Func<FinalizeBatchRunActivity, FinalizeBatchRunInput, CancellationToken, Task<bool>>>(), ct);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);
    }
}
