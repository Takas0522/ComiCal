using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Tests.Unit.Batch;

/// <summary>
/// Replays <see cref="DailyReleaseOrchestrator"/> against a substituted
/// <see cref="TaskOrchestrationContext"/> and asserts the deterministic activity
/// call sequence (StartBatchRun → FetchRakutenPage × N → UpsertSeriesAndVolume × M
/// → EnsureCoverThumbnail × K → FinishBatchRun).
/// </summary>
public sealed class OrchestratorDeterminismTests
{
    [Fact]
    public async Task Happy_path_calls_activities_in_deterministic_sequence()
    {
        var runId = Guid.CreateVersion7();
        var input = new BatchRunInput("コミック", MaxPages: 3, RunId: runId);
        var context = Substitute.For<TaskOrchestrationContext>();
        context.GetInput<BatchRunInput>().Returns(input);
        context.CreateReplaySafeLogger(Arg.Any<string>()).Returns(NullLogger.Instance);

        // StartBatchRun and FinishBatchRun are non-generic CallActivityAsync.
        context.CallActivityAsync(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.CompletedTask);

        // FetchRakutenPage returns 2 payloads per page; one with cover, one without.
        var payloadWithCover = MakePayload("9784088100005", coverUrl: "https://img/a.jpg");
        var payloadWithoutCover = MakePayload("9784088100012", coverUrl: null);
        context.CallActivityAsync<IReadOnlyList<BatchVolumePayload>>(
                Arg.Is<TaskName>(t => t.Name == "FetchRakutenPage"),
                Arg.Any<object?>(),
                Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult<IReadOnlyList<BatchVolumePayload>>(
                [payloadWithCover, payloadWithoutCover]));

        // UpsertSeriesAndVolume returns a fresh result each call.
        context.CallActivityAsync<UpsertResult>(
                Arg.Is<TaskName>(t => t.Name == "UpsertSeriesAndVolume"),
                Arg.Any<object?>(),
                Arg.Any<TaskOptions?>())
            .Returns(call =>
            {
                var p = (BatchVolumePayload)call.Args()[1]!;
                return Task.FromResult(new UpsertResult(
                    Guid.CreateVersion7(),
                    IsNew: true,
                    p.Isbn,
                    p.CoverImageUrl,
                    CurrentCoverHash: null));
            });

        var summary = await DailyReleaseOrchestrator.RunOrchestrator(context);

        summary.FetchedItems.Should().Be(6);   // 3 pages × 2 payloads
        summary.UpsertedVolumes.Should().Be(6);
        summary.FailedItems.Should().Be(0);

        Received.InOrder(() =>
        {
            context.CallActivityAsync(
                Arg.Is<TaskName>(t => t.Name == "StartBatchRun"),
                Arg.Any<object?>(),
                Arg.Any<TaskOptions?>());
        });

        await context.Received(3).CallActivityAsync<IReadOnlyList<BatchVolumePayload>>(
            Arg.Is<TaskName>(t => t.Name == "FetchRakutenPage"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
        await context.Received(6).CallActivityAsync<UpsertResult>(
            Arg.Is<TaskName>(t => t.Name == "UpsertSeriesAndVolume"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());

        // Only payloads with a cover URL are dispatched to EnsureCoverThumbnail (3 pages × 1 with-cover).
        await context.Received(3).CallActivityAsync(
            Arg.Is<TaskName>(t => t.Name == "EnsureCoverThumbnail"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());

        await context.Received(1).CallActivityAsync(
            Arg.Is<TaskName>(t => t.Name == "FinishBatchRun"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
        await context.DidNotReceive().CallActivityAsync(
            Arg.Is<TaskName>(t => t.Name == "FailBatchRun"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    [Fact]
    public async Task Fetch_failure_calls_FailBatchRun_and_rethrows()
    {
        var runId = Guid.CreateVersion7();
        var input = new BatchRunInput("コミック", MaxPages: 2, RunId: runId);
        var context = Substitute.For<TaskOrchestrationContext>();
        context.GetInput<BatchRunInput>().Returns(input);
        context.CreateReplaySafeLogger(Arg.Any<string>()).Returns(NullLogger.Instance);

        context.CallActivityAsync(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.CompletedTask);

        var failure = new InvalidOperationException("rakuten 503");
        context.CallActivityAsync<IReadOnlyList<BatchVolumePayload>>(
                Arg.Is<TaskName>(t => t.Name == "FetchRakutenPage"),
                Arg.Any<object?>(),
                Arg.Any<TaskOptions?>())
            .Returns<Task<IReadOnlyList<BatchVolumePayload>>>(_ => Task.FromException<IReadOnlyList<BatchVolumePayload>>(failure));

        var act = async () => await DailyReleaseOrchestrator.RunOrchestrator(context);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await context.Received(1).CallActivityAsync(
            Arg.Is<TaskName>(t => t.Name == "FailBatchRun"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
        await context.DidNotReceive().CallActivityAsync(
            Arg.Is<TaskName>(t => t.Name == "FinishBatchRun"),
            Arg.Any<object?>(),
            Arg.Any<TaskOptions?>());
    }

    private static BatchVolumePayload MakePayload(string isbn, string? coverUrl) => new(
        Isbn: isbn,
        Title: "T",
        SeriesName: "S",
        SeriesNameKana: string.Empty,
        VolumeNumber: 1,
        ReleaseDate: new DateOnly(2026, 4, 1),
        ReleaseDateIsMonthOnly: false,
        AuthorName: "尾田 栄一郎",
        AuthorKana: string.Empty,
        PublisherName: "集英社",
        ItemUrl: null,
        CoverImageUrl: coverUrl);
}
