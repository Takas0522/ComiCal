using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Orchestrators;

public sealed class FetchOrchestratorTests
{
    private static readonly DateOnly DateFrom = new(2025, 1, 1);
    private static readonly DateOnly DateTo = new(2025, 12, 31);

    private static TaskOrchestrationContext BuildContext(
        FetchPageOutput fetchOutput,
        UpsertVolumesOutput upsertOutput)
    {
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CreateReplaySafeLogger(Arg.Any<string>())
            .Returns(NullLogger.Instance);
        context.CallActivityAsync<FetchPageOutput>(
                Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(fetchOutput));
        context.CallActivityAsync<UpsertVolumesOutput>(
                Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(upsertOutput));
        return context;
    }

    private static FetchInput MakeInput(Guid batchRunId, int page, int accumFetched = 0, int accumUpserted = 0)
        => new(batchRunId, page, DateFrom, DateTo, accumFetched, accumUpserted, []);

    // ── ContinueAsNew when more pages remain ─────────────────────────────────

    [Fact]
    public async Task RunAsync_MorePagesExist_CallsContinueAsNew()
    {
        var fetchOutput = new FetchPageOutput(TotalPages: 3, FetchedCount: 30, Items: []);
        var upsertOutput = new UpsertVolumesOutput(UpsertedCount: 28, ThumbnailPending: [], FailedIsbn13s: []);
        var context = BuildContext(fetchOutput, upsertOutput);
        context.GetInput<FetchInput>().Returns(MakeInput(Guid.NewGuid(), page: 1));

        _ = await FetchOrchestrator.Run(context);

        context.Received(1).ContinueAsNew(Arg.Any<object?>(), false);
    }

    [Fact]
    public async Task RunAsync_ContinueAsNew_IncrementedPagePassedAsNewInput()
    {
        var fetchOutput = new FetchPageOutput(TotalPages: 5, FetchedCount: 30, Items: []);
        var upsertOutput = new UpsertVolumesOutput(UpsertedCount: 30, ThumbnailPending: [], FailedIsbn13s: []);
        var context = BuildContext(fetchOutput, upsertOutput);
        context.GetInput<FetchInput>().Returns(MakeInput(Guid.NewGuid(), page: 2, accumFetched: 30, accumUpserted: 30));

        _ = await FetchOrchestrator.Run(context);

        context.Received(1).ContinueAsNew(
            Arg.Is<object?>(o => o != null && ((FetchInput)o).Page == 3),
            false);
    }

    // ── Last page — no ContinueAsNew, returns FetchSummary ───────────────────

    [Fact]
    public async Task RunAsync_LastPage_DoesNotCallContinueAsNew()
    {
        var fetchOutput = new FetchPageOutput(TotalPages: 1, FetchedCount: 10, Items: []);
        var upsertOutput = new UpsertVolumesOutput(UpsertedCount: 9, ThumbnailPending: [], FailedIsbn13s: ["9784088726236"]);
        var context = BuildContext(fetchOutput, upsertOutput);
        context.GetInput<FetchInput>().Returns(MakeInput(Guid.NewGuid(), page: 1));

        var result = await FetchOrchestrator.Run(context);

        context.DidNotReceive().ContinueAsNew(Arg.Any<object?>(), Arg.Any<bool>());
        Assert.NotNull(result);
        Assert.Equal(10, result.FetchedCount);
        Assert.Equal(9, result.UpsertedCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public async Task RunAsync_LastPage_AccumulatedCountsAddedToThisPage()
    {
        var fetchOutput = new FetchPageOutput(TotalPages: 3, FetchedCount: 5, Items: []);
        var upsertOutput = new UpsertVolumesOutput(UpsertedCount: 4, ThumbnailPending: [], FailedIsbn13s: []);
        var context = BuildContext(fetchOutput, upsertOutput);

        // Simulate being called on the 3rd (final) page with accumulated counts
        var input = MakeInput(Guid.NewGuid(), page: 3, accumFetched: 60, accumUpserted: 56);
        context.GetInput<FetchInput>().Returns(input);
        var result = await FetchOrchestrator.Run(context);

        Assert.Equal(65, result.FetchedCount);   // 60 + 5
        Assert.Equal(60, result.UpsertedCount);  // 56 + 4
    }

    // ── Thumbnail accumulation ────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_LastPage_AccumulatedThumbnailsMergedWithCurrentPage()
    {
        var pendingFromCurrentPage = new List<ThumbnailPendingItem>
        {
            new(Guid.NewGuid(), "https://img.example.com/new.jpg", null),
        };
        var fetchOutput = new FetchPageOutput(TotalPages: 2, FetchedCount: 1, Items: []);
        var upsertOutput = new UpsertVolumesOutput(1, pendingFromCurrentPage, []);

        var existingThumbnail = new ThumbnailPendingItem(Guid.NewGuid(), "https://img.example.com/old.jpg", null);
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CreateReplaySafeLogger(Arg.Any<string>()).Returns(NullLogger.Instance);
        context.CallActivityAsync<FetchPageOutput>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(fetchOutput));
        context.CallActivityAsync<UpsertVolumesOutput>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(_ => Task.FromResult(upsertOutput));

        var input = new FetchInput(Guid.NewGuid(), Page: 2, DateFrom, DateTo, 10, 9, [existingThumbnail]);
        context.GetInput<FetchInput>().Returns(input);
        var result = await FetchOrchestrator.Run(context);

        // 1 from accumulated + 1 from this page
        Assert.Equal(2, result.ThumbnailPending.Count);
    }
}
