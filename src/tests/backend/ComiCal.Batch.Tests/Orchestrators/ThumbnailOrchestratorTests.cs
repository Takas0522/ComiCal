using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Orchestrators;

public sealed class ThumbnailOrchestratorTests
{
    private static ThumbnailInput MakeInput(int count, Guid? batchRunId = null)
    {
        var items = Enumerable.Range(0, count)
            .Select(_ => new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/1.jpg", null))
            .ToList();
        return new ThumbnailInput(batchRunId ?? Guid.NewGuid(), items);
    }

    private static TaskOrchestrationContext BuildContext(Func<DownloadThumbnailOutput> outputFactory)
    {
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CreateReplaySafeLogger(Arg.Any<string>())
            .Returns(NullLogger.Instance);
        context.CallActivityAsync<DownloadThumbnailOutput>(
                Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(outputFactory()));
        return context;
    }

    // ── fan-out: activity called once per item ───────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]   // exactly one batch
    [InlineData(9)]   // one full batch + one partial
    [InlineData(16)]  // exactly two batches
    public async Task RunAsync_NItems_CallsDownloadThumbnailActivityNTimes(int itemCount)
    {
        var context = BuildContext(() => new DownloadThumbnailOutput(true, false, false, null));

        var result = await new ThumbnailOrchestrator().RunAsync(context, MakeInput(itemCount));

        await context.Received(itemCount).CallActivityAsync<DownloadThumbnailOutput>(
            Arg.Is<TaskName>(n => n.Name == "DownloadThumbnailActivity"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
        Assert.Equal(itemCount, result.DownloadedCount);
    }

    // ── empty input ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_EmptyItems_NoActivityCallsAndZeroCounts()
    {
        var context = BuildContext(() => new DownloadThumbnailOutput(true, false, false, null));

        var result = await new ThumbnailOrchestrator().RunAsync(context, MakeInput(0));

        await context.DidNotReceive().CallActivityAsync<DownloadThumbnailOutput>(
            Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>());
        Assert.Equal(0, result.DownloadedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
    }

    // ── aggregate counters ────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_MixedResults_AggregatesToDownloadedSkippedFailed()
    {
        var outputs = new Queue<DownloadThumbnailOutput>([
            new(true,  false, false, null),    // downloaded
            new(false, true,  false, null),    // skipped
            new(false, false, true,  "error"), // failed
        ]);

        var context = Substitute.For<TaskOrchestrationContext>();
        context.CreateReplaySafeLogger(Arg.Any<string>()).Returns(NullLogger.Instance);
        context.CallActivityAsync<DownloadThumbnailOutput>(
                Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(outputs.Dequeue()));

        var result = await new ThumbnailOrchestrator().RunAsync(context, MakeInput(3));

        Assert.Equal(1, result.DownloadedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);
    }

    // ── correct VolumeId is forwarded to the activity ─────────────────────────

    [Fact]
    public async Task RunAsync_ForwardsVolumeIdToActivity()
    {
        var expectedVolumeId = Guid.NewGuid();
        var input = new ThumbnailInput(Guid.NewGuid(), [
            new ThumbnailPendingItem(expectedVolumeId, "https://img.example.com/1.jpg", null),
        ]);

        var context = BuildContext(() => new DownloadThumbnailOutput(true, false, false, null));

        await new ThumbnailOrchestrator().RunAsync(context, input);

        await context.Received(1).CallActivityAsync<DownloadThumbnailOutput>(
            Arg.Any<TaskName>(),
            Arg.Is<object?>(o => o != null && ((DownloadThumbnailInput)o).VolumeId == expectedVolumeId),
            Arg.Any<TaskOptions?>());
    }
}
