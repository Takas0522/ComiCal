using ComiCal.Batch.Models;
using ComiCal.Batch.Observability;
using ComiCal.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Marks the <c>BatchRuns</c> row as <c>Succeeded</c> with the final aggregate counts and
/// emits the <c>batch.volumes_ingested</c> + <c>batch.duration_seconds</c> custom metrics.
/// </summary>
public sealed class FinishBatchRunActivity(
    ComiCalDbContext db,
    IBatchMetrics metrics,
    ILogger<FinishBatchRunActivity> logger)
{
    private readonly ComiCalDbContext _db = db;
    private readonly IBatchMetrics _metrics = metrics;
    private readonly ILogger<FinishBatchRunActivity> _logger = logger;

    [Function("FinishBatchRun")]
    public async Task RunAsync([ActivityTrigger] FinishBatchRunInput input, FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        const string sql = @"
UPDATE dbo.BatchRuns
   SET Status = N'Succeeded',
       CompletedAt = SYSUTCDATETIME(),
       FetchedItemCount = @Fetched,
       UpsertedVolumeCount = @Upserted,
       FailedItemCount = @Failed,
       UpdatedAt = SYSUTCDATETIME()
 OUTPUT inserted.StartedAt, inserted.CompletedAt
 WHERE BatchRunId = @RunId;";

        DateTime? startedAt = null;
        DateTime? completedAt = null;

        var conn = _db.Database.GetDbConnection();
        await _db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@Fetched", input.Summary.FetchedItems));
            cmd.Parameters.Add(new SqlParameter("@Upserted", input.Summary.UpsertedVolumes));
            cmd.Parameters.Add(new SqlParameter("@Failed", input.Summary.FailedItems));
            cmd.Parameters.Add(new SqlParameter("@RunId", input.RunId));

            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                startedAt = reader.GetDateTime(0);
                completedAt = reader.GetDateTime(1);
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }

        _metrics.RecordVolumesIngested(input.Summary.UpsertedVolumes);
        if (startedAt is not null && completedAt is not null)
        {
            _metrics.RecordOrchestrationDuration(completedAt.Value - startedAt.Value);
        }

        _logger.LogInformation(
            "BatchRun {RunId} finished: fetched={Fetched}, upserted={Upserted}, failed={Failed}, durationSeconds={DurationSeconds}",
            input.RunId,
            input.Summary.FetchedItems,
            input.Summary.UpsertedVolumes,
            input.Summary.FailedItems,
            (startedAt is not null && completedAt is not null)
                ? (completedAt.Value - startedAt.Value).TotalSeconds
                : 0);
    }
}
