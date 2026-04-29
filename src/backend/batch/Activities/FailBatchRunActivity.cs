using ComiCal.Batch.Models;
using ComiCal.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

/// <summary>
/// Marks the <c>BatchRuns</c> row as <c>Failed</c> (truncating the error to 4000 chars
/// to fit downstream logging constraints) and writes one or more <c>FailedItems</c>
/// rows for the dead-letter / alerting pipeline.
/// </summary>
public sealed class FailBatchRunActivity(ComiCalDbContext db, ILogger<FailBatchRunActivity> logger)
{
    private const int MaxErrorLength = 4000;

    private readonly ComiCalDbContext _db = db;
    private readonly ILogger<FailBatchRunActivity> _logger = logger;

    [Function("FailBatchRun")]
    public async Task RunAsync([ActivityTrigger] FailBatchRunInput input, FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(executionContext);
        var ct = executionContext.CancellationToken;

        var truncated = input.Error.Length > MaxErrorLength
            ? input.Error[..MaxErrorLength]
            : input.Error;

        const string updateSql = @"
UPDATE dbo.BatchRuns
   SET Status = N'Failed',
       CompletedAt = SYSUTCDATETIME(),
       FailedItemCount = @Failed,
       UpdatedAt = SYSUTCDATETIME()
 WHERE BatchRunId = @RunId;";
        await _db.Database.ExecuteSqlRawAsync(
            updateSql,
            [
                new SqlParameter("@Failed", input.Failed.Count),
                new SqlParameter("@RunId", input.RunId),
            ],
            ct).ConfigureAwait(false);

        const string insertFailedSql = @"
INSERT INTO dbo.FailedItems (FailedItemId, BatchRunId, ItemKey, Reason, PayloadJson, CreatedAt, UpdatedAt)
VALUES (@Id, @RunId, @ItemKey, @Reason, @Payload, SYSUTCDATETIME(), SYSUTCDATETIME());";

        if (input.Failed.Count == 0)
        {
            // Always record at least one FailedItems row when the orchestrator failed.
            await _db.Database.ExecuteSqlRawAsync(
                insertFailedSql,
                [
                    new SqlParameter("@Id", Guid.CreateVersion7()),
                    new SqlParameter("@RunId", input.RunId),
                    new SqlParameter("@ItemKey", "orchestrator"),
                    new SqlParameter("@Reason", Truncate(truncated, 1024)),
                    new SqlParameter("@Payload", (object?)null ?? DBNull.Value),
                ],
                ct).ConfigureAwait(false);
        }
        else
        {
            foreach (var item in input.Failed)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    insertFailedSql,
                    [
                        new SqlParameter("@Id", Guid.CreateVersion7()),
                        new SqlParameter("@RunId", input.RunId),
                        new SqlParameter("@ItemKey", Truncate(item.ItemKey, 256)),
                        new SqlParameter("@Reason", Truncate(item.Reason, 1024)),
                        new SqlParameter("@Payload", (object?)item.PayloadJson ?? DBNull.Value),
                    ],
                    ct).ConfigureAwait(false);
            }
        }

        _logger.LogError(
            "BatchRun {RunId} failed: {Error} ({FailedCount} failed items)",
            input.RunId,
            truncated,
            input.Failed.Count);
    }

    private static string Truncate(string value, int max) =>
        value.Length > max ? value[..max] : value;
}
