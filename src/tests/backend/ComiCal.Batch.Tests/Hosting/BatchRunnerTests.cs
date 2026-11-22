using ComiCal.Batch.Hosting;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class BatchRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 16, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Start = new(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);
    private readonly ISingleInstanceGuard guard = Substitute.For<ISingleInstanceGuard>();
    private readonly IStaleBatchRunCleaner cleaner = Substitute.For<IStaleBatchRunCleaner>();
    private readonly IBatchRunRepository runs = Substitute.For<IBatchRunRepository>();
    private readonly IPreflight preflight = Substitute.For<IPreflight>();
    private readonly IBatchWork work = Substitute.For<IBatchWork>();
    private readonly IFailureNotifier notifier = Substitute.For<IFailureNotifier>();
    private readonly IHeartbeat heartbeat = Substitute.For<IHeartbeat>();
    private readonly TestClock clock = new();
    private readonly BatchRunner runner;

    public BatchRunnerTests()
    {
        guard.RunExclusive(Arg.Any<Func<int>>()).Returns(call => call.Arg<Func<int>>()());
        preflight.RunAsync(Arg.Any<CancellationToken>()).Returns(true);
        var completed = BatchRun.Create();
        completed.Complete(4, 3, 2, 1, 0);
        work.FetchAsync(Arg.Any<CancellationToken>()).Returns(completed.BatchRunId);
        runs.FindByIdAsync(completed.BatchRunId, Arg.Any<CancellationToken>()).Returns(completed);
        runner = new(guard, new(() => cleaner), new(() => runs), preflight, work, notifier, heartbeat,
            new ConfigurationBuilder().Build(), clock, NullLogger<BatchRunner>.Instance);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Execute_Run_ChecksJstDayAndHonorsForce(bool alreadyCompleted, bool force)
    {
        runs.HasSucceededAsync(Start, Start.AddDays(1), Arg.Any<CancellationToken>()).Returns(alreadyCompleted);

        Assert.Equal(ExitCodes.Success, Run(force));

        await cleaner.Received(1).CleanAsync(Now.UtcDateTime.AddHours(-6), Arg.Any<CancellationToken>());
        await runs.Received(force ? 0 : 1).HasSucceededAsync(Start, Start.AddDays(1), Arg.Any<CancellationToken>());
        var executions = alreadyCompleted && !force ? 0 : 1;
        await work.Received(executions).WarmupAsync(Arg.Any<CancellationToken>());
        await work.Received(executions).FetchAsync(Arg.Any<CancellationToken>());
        await heartbeat.Received(1).SendAsync(ExitCodes.Success);
        Assert.Empty(notifier.ReceivedCalls());
    }

    [Theory]
    [InlineData(ExitCodes.Failure)]
    [InlineData(ExitCodes.PreflightFailure)]
    [InlineData(ExitCodes.Timeout)]
    [InlineData(ExitCodes.AlreadyRunning)]
    public async Task Execute_UnsuccessfulRun_ReportsExpectedExitCode(int expected)
    {
        switch (expected)
        {
            case ExitCodes.Failure:
                work.FetchAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<Guid>(new InvalidOperationException()));
                break;
            case ExitCodes.PreflightFailure:
                preflight.RunAsync(Arg.Any<CancellationToken>()).Returns(false);
                break;
            case ExitCodes.Timeout:
                work.FetchAsync(Arg.Any<CancellationToken>()).Returns(call =>
                {
                    clock.FireTimeout();
                    call.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Guid.Empty;
                });
                break;
            case ExitCodes.AlreadyRunning:
                guard.RunExclusive(Arg.Any<Func<int>>()).Returns(ExitCodes.AlreadyRunning);
                break;
        }

        Assert.Equal(expected, Run());
        if (expected is ExitCodes.PreflightFailure or ExitCodes.AlreadyRunning)
            Assert.Empty(cleaner.ReceivedCalls());
        if (expected == ExitCodes.AlreadyRunning)
        {
            Assert.Empty(notifier.ReceivedCalls());
            Assert.Empty(heartbeat.ReceivedCalls());
        }
        else
        {
            await notifier.Received(1).SendAsync(expected);
            await heartbeat.Received(1).SendAsync(expected);
        }
    }

    [Fact]
    public async Task Execute_CompletedRunWithItemFailures_ReturnsSuccessWithoutFailureNotification()
    {
        var partial = BatchRun.Create();
        partial.Complete(4, 3, 1, 0, 2);
        work.FetchAsync(Arg.Any<CancellationToken>()).Returns(partial.BatchRunId);
        runs.FindByIdAsync(partial.BatchRunId, Arg.Any<CancellationToken>()).Returns(partial);

        Assert.Equal(ExitCodes.Success, Run(force: true));

        await heartbeat.Received(1).SendAsync(ExitCodes.Success);
        Assert.Empty(notifier.ReceivedCalls());
    }

    [Fact]
    public void Execute_ExternalCancellation_ReturnsFailure()
    {
        using var external = new CancellationTokenSource();
        work.FetchAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            external.Cancel();
            call.Arg<CancellationToken>().ThrowIfCancellationRequested();
            return Guid.Empty;
        });

        Assert.Equal(ExitCodes.Failure, Run(ct: external.Token));
    }

    [Fact]
    public void Execute_Status_PrintsCountsAndUsesSpecifiedJstDay()
    {
        var item = BatchRun.Create();
        item.Complete(4, 3, 2, 1, 0);
        runs.ListStartedAsync(Start, Start.AddDays(1), Arg.Any<CancellationToken>()).Returns([item]);
        using var output = new StringWriter();

        Assert.Equal(ExitCodes.Success, runner.Execute(new(BatchCommandKind.Status, Date: new(2026, 11, 22)), output, default));
        Assert.Contains("2026-11-22 (JST)", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("status=Succeeded fetched=4 upserted=3 downloaded=2 skipped=1 failed=0", output.ToString(), StringComparison.Ordinal);
        Assert.Empty(heartbeat.ReceivedCalls());
        Assert.Empty(notifier.ReceivedCalls());
    }

    [Theory]
    [InlineData(true, ExitCodes.Success)]
    [InlineData(false, ExitCodes.PreflightFailure)]
    public void Execute_Preflight_DoesNotSendHeartbeatOrNotification(bool passes, int expected)
    {
        preflight.RunAsync(Arg.Any<CancellationToken>()).Returns(passes);

        Assert.Equal(expected, runner.Execute(new(BatchCommandKind.Preflight), TextWriter.Null, default));
        Assert.Empty(heartbeat.ReceivedCalls());
        Assert.Empty(notifier.ReceivedCalls());
    }

    private int Run(bool force = false, CancellationToken ct = default)
        => runner.Execute(new(BatchCommandKind.Run, force), TextWriter.Null, ct);

    private sealed class TestClock : TimeProvider
    {
        private Action? fire;
        public override DateTimeOffset GetUtcNow() => Now;
        public void FireTimeout() => fire!();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            fire = () => callback(state);
            return TimeProvider.System.CreateTimer(callback, state, Timeout.InfiniteTimeSpan, period);
        }
    }
}
