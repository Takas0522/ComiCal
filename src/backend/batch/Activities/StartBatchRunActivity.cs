using ComiCal.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Inserts (idempotently) a <c>BatchRuns</c> row with status <c>Running</c>.
/// Re-execution on the same <c>RunId</c> is a no-op.
/// </summary>
public sealed class StartBatchRunActivity(ComiCalDbContext db, ILogger<StartBatchRunActivity> logger)
{
    private readonly ComiCalDbContext _db = db;
    private readonly ILogger<StartBatchRunActivity> _logger = logger;

    [Function("StartBatchRun")]
    public async Task RunAsync([ActivityTrigger] Guid runId, FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.BatchRuns WHERE BatchRunId = @RunId)
BEGIN
    INSERT INTO dbo.BatchRuns
        (BatchRunId, StartedAt, Status, FetchedItemCount, UpsertedVolumeCount, FailedItemCount, CreatedAt, UpdatedAt)
    VALUES
        (@RunId, SYSUTCDATETIME(), N'Running', 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
END";

        await _db.Database
            .ExecuteSqlRawAsync(sql, [new SqlParameter("@RunId", runId)], ct)
            .ConfigureAwait(false);

        _logger.LogInformation("BatchRun {RunId} started", runId);
    }
}
