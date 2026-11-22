using System.Globalization;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Hosting;

public sealed partial class BatchRunner(
    ISingleInstanceGuard guard, Lazy<IStaleBatchRunCleaner> cleaner, Lazy<IBatchRunRepository> runs,
    IPreflight preflight, IBatchWork work, IFailureNotifier notifier, IHeartbeat heartbeat,
    IConfiguration configuration, TimeProvider clock, ILogger<BatchRunner> logger)
{
    private static readonly TimeSpan JapanOffset = TimeSpan.FromHours(9);

    public int Execute(BatchCommand? command, TextWriter output, CancellationToken ct)
    {
        int result;
        try
        {
            result = command?.Kind switch
            {
                // Mutex acquisition and release must happen on the same thread.
                BatchCommandKind.Run => guard.RunExclusive(() => RunAsync(command.Force, ct).GetAwaiter().GetResult()),
                BatchCommandKind.Status => StatusAsync(command.Date, output, ct).GetAwaiter().GetResult(),
                BatchCommandKind.Preflight => CheckAsync(ct).GetAwaiter().GetResult(),
                _ => InvalidArguments(output)
            };
        }
        catch (Exception ex)
        {
            LogFailure(logger, ex);
            result = ExitCodes.Failure;
        }

        var summary = ExitCodes.Summary(result);
        LogResult(logger, result, summary);
        // 通知とハートビートは定期実行（run）の結果だけを送る。status / preflight などの診断コマンドで
        // 外部監視の期限がリセットされ、実行漏れが隠れるのを防ぐ。
        if (command?.Kind == BatchCommandKind.Run)
        {
            if (ExitCodes.IsFailure(result))
                notifier.SendAsync(result).GetAwaiter().GetResult();
            if (result == ExitCodes.Success || ExitCodes.IsFailure(result))
                heartbeat.SendAsync(result).GetAwaiter().GetResult();
        }

        return result;
    }

    private async Task<int> RunAsync(bool force, CancellationToken externalCancellation)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromMinutes(configuration.GetValue<int?>("Batch:TimeoutMinutes") ?? 240), clock);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(externalCancellation, timeout.Token);
        var ct = cancellation.Token;

        try
        {
            // Preflight validates and resumes SQL before stale cleanup and the completion query need it.
            if (await CheckAsync(ct) != ExitCodes.Success)
                return ExitCodes.PreflightFailure;

            var now = clock.GetUtcNow();
            await cleaner.Value.CleanAsync(now.UtcDateTime.AddMinutes(-(configuration.GetValue<int?>("Batch:StaleAfterMinutes") ?? 360)), ct);
            var (start, end) = UtcRange(Today(now));
            if (!force && await runs.Value.HasSucceededAsync(start, end, ct))
            {
                ct.ThrowIfCancellationRequested();
                LogAlreadyCompleted(logger);
                return ExitCodes.Success;
            }

            await work.WarmupAsync(ct);
            var id = await work.FetchAsync(ct);
            var run = await runs.Value.FindByIdAsync(id, ct);
            ct.ThrowIfCancellationRequested();
            // 処理が最後まで完了していれば、一部の項目（例: サムネイル 1 件）の失敗は成功扱いにする（2026-11-22 決定）。
            // BatchRun の Status はドメインの規則どおり Failed のままなので、件数は警告ログと管理画面で確認する。
            if (run is { FailedItemCount: > 0 })
                LogItemFailures(logger, run.BatchRunId, run.FailedItemCount);
            return ExitCodes.Success;
        }
        catch (Exception ex) when (ct.IsCancellationRequested)
        {
            LogFailure(logger, ex);
            return externalCancellation.IsCancellationRequested ? ExitCodes.Failure : ExitCodes.Timeout;
        }
    }

    private async Task<int> CheckAsync(CancellationToken ct)
    {
        var passed = await preflight.RunAsync(ct);
        ct.ThrowIfCancellationRequested();
        return passed ? ExitCodes.Success : ExitCodes.PreflightFailure;
    }

    private async Task<int> StatusAsync(DateOnly? date, TextWriter output, CancellationToken ct)
    {
        var day = date ?? Today(clock.GetUtcNow());
        var (start, end) = UtcRange(day);
        var items = await runs.Value.ListStartedAsync(start, end, ct);
        output.WriteLine(FormattableString.Invariant($"Batch runs for {day:yyyy-MM-dd} (JST): {items.Count}"));
        foreach (var run in items)
        {
            var started = InJapan(run.StartedAt);
            var completed = run.CompletedAt is { } value ? InJapan(value) : "-";
            output.WriteLine(FormattableString.Invariant(
                $"{run.BatchRunId} started={started} completed={completed} status={run.Status} fetched={run.FetchedItemCount} upserted={run.UpsertedVolumeCount} downloaded={run.DownloadedThumbnailCount} skipped={run.SkippedThumbnailCount} failed={run.FailedItemCount}"));
        }

        ct.ThrowIfCancellationRequested();
        return ExitCodes.Success;
    }

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.ToOffset(JapanOffset).DateTime);

    private static (DateTime Start, DateTime End) UtcRange(DateOnly day)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var start = day == DateOnly.MinValue ? midnight : midnight.Subtract(JapanOffset);
        var end = midnight.Add(TimeSpan.FromDays(1) - JapanOffset);
        return (start, end);
    }

    private static string InJapan(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
        .ToOffset(JapanOffset).ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

    private static int InvalidArguments(TextWriter output)
    {
        output.WriteLine(BatchCommand.Usage);
        return ExitCodes.PreflightFailure;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Batch exit code {ExitCode}. {Summary}")]
    private static partial void LogResult(ILogger logger, int exitCode, string summary);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Batch run {BatchRunId} completed with {FailedItemCount} failed item(s); see /admin/batch-runs.")]
    private static partial void LogItemFailures(ILogger logger, Guid batchRunId, int failedItemCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Today's batch already succeeded (JST).")]
    private static partial void LogAlreadyCompleted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Batch processing stopped.")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
