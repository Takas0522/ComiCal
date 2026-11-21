using ComiCal.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Hosting;

/// <summary>Resumes Azure SQL Serverless before the daily fetch.</summary>
public partial class SqlWarmup(ComiCalDbContext dbContext, ILogger<SqlWarmup> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        LogWarmupStarted(logger);

        try
        {
            // DB を叩いて auto-resume をトリガーするだけが目的の軽量クエリ。
            // 結果自体は使わないため、存在確認 (AnyAsync) で十分。
            await dbContext.BatchRuns.AnyAsync(ct);
            LogWarmupSucceeded(logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The batch SQL retry policy can still absorb auto-resume delays.
            LogWarmupFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SQL warmup started.")]
    private static partial void LogWarmupStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Warmup query succeeded; database should be resumed.")]
    private static partial void LogWarmupSucceeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warmup query failed; database may still be resuming. This is non-fatal.")]
    private static partial void LogWarmupFailed(ILogger logger, Exception exception);
}
