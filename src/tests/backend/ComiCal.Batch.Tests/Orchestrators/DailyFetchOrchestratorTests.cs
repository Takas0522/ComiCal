using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Orchestrators;

public sealed class DailyFetchOrchestratorTests
{
    /// <summary>Returns a context substitute pre-wired with the common happy-path setup.</summary>
    private static TaskOrchestrationContext BuildContext(
        Guid batchRunId,
        FetchSummary fetchSummary,
        ThumbnailSummary? thumbSummary = null)
    {
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CurrentUtcDateTime
            .Returns(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        context.CreateReplaySafeLogger(Arg.Any<string>())
            .Returns(NullLogger.Instance);

        context.CallActivityAsync<Guid>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(batchRunId));
        context.CallActivityAsync<bool>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(true));

        context.CallSubOrchestratorAsync<FetchSummary>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(fetchSummary));

        if (thumbSummary is not null)
            context.CallSubOrchestratorAsync<ThumbnailSummary>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
                .Returns(Task.FromResult(thumbSummary));

        return context;
    }

    [Fact]
    public async Task RunAsync_ReturnsBatchRunIdAsString()
    {
        var batchRunId = Guid.NewGuid();
        var context = BuildContext(batchRunId, new FetchSummary(50, 40, 0, []));

        var result = await DailyFetchOrchestrator.Run(context);

        Assert.Equal(batchRunId.ToString(), result);
    }

    [Fact]
    public async Task RunAsync_AlwaysCallsCreateBatchRunActivity()
    {
        var batchRunId = Guid.NewGuid();
        var context = BuildContext(batchRunId, new FetchSummary(0, 0, 0, []));

        await DailyFetchOrchestrator.Run(context);

        await context.Received(1).CallActivityAsync<Guid>(
            Arg.Is<TaskName>(n => n.Name == "CreateBatchRunActivity"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task RunAsync_AlwaysCallsFetchOrchestrator()
    {
        var batchRunId = Guid.NewGuid();
        var context = BuildContext(batchRunId, new FetchSummary(0, 0, 0, []));

        await DailyFetchOrchestrator.Run(context);

        await context.Received(1).CallSubOrchestratorAsync<FetchSummary>(
            Arg.Is<TaskName>(n => n.Name == "FetchOrchestrator"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task RunAsync_AlwaysCallsFinalizeBatchRunActivity()
    {
        var batchRunId = Guid.NewGuid();
        var context = BuildContext(batchRunId, new FetchSummary(0, 0, 0, []));

        await DailyFetchOrchestrator.Run(context);

        await context.Received(1).CallActivityAsync<bool>(
            Arg.Is<TaskName>(n => n.Name == "FinalizeBatchRunActivity"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task RunAsync_WithThumbnailPending_CallsThumbnailOrchestrator()
    {
        var batchRunId = Guid.NewGuid();
        var thumbnails = new List<ThumbnailPendingItem>
        {
            new(Guid.NewGuid(), "https://img.example.com/1.jpg", null),
        };
        var fetchSummary = new FetchSummary(10, 8, 0, thumbnails);
        var thumbSummary = new ThumbnailSummary(1, 0, 0);
        var context = BuildContext(batchRunId, fetchSummary, thumbSummary);

        await DailyFetchOrchestrator.Run(context);

        await context.Received(1).CallSubOrchestratorAsync<ThumbnailSummary>(
            Arg.Is<TaskName>(n => n.Name == "ThumbnailOrchestrator"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task RunAsync_NoThumbnailPending_SkipsThumbnailOrchestrator()
    {
        var batchRunId = Guid.NewGuid();
        var context = BuildContext(batchRunId, new FetchSummary(10, 10, 0, []));

        await DailyFetchOrchestrator.Run(context);

        await context.DidNotReceive().CallSubOrchestratorAsync<ThumbnailSummary>(
            Arg.Any<TaskName>(),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }
}
