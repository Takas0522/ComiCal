using ComiCal.Batch.Models;
using ComiCal.Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Activities;

public partial class FinalizeBatchRunActivity(
    IBatchRunRepository batchRunRepo,
    ILogger<FinalizeBatchRunActivity> logger)
{
    [Function("FinalizeBatchRunActivity")]
    public async Task<bool> Run([ActivityTrigger] FinalizeBatchRunInput input)
    {
        var batchRun = await batchRunRepo.FindByIdAsync(input.BatchRunId);
        if (batchRun is null)
        {
            LogBatchRunNotFound(logger, input.BatchRunId);
            return false;
        }

        batchRun.Complete(
            input.FetchedItemCount,
            input.UpsertedVolumeCount,
            input.DownloadedCount,
            input.SkippedCount,
            input.FailedCount);

        await batchRunRepo.UpdateAsync(batchRun);

        LogBatchRunFinalized(logger, input.BatchRunId, input.FetchedItemCount,
            input.UpsertedVolumeCount, input.DownloadedCount, input.FailedCount);

        return true;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "BatchRun {BatchRunId} not found for finalization")]
    private static partial void LogBatchRunNotFound(ILogger logger, Guid batchRunId);

    [LoggerMessage(Level = LogLevel.Information, Message = "BatchRun {BatchRunId} finalized: fetched={Fetched}, upserted={Upserted}, downloaded={Downloaded}, failed={Failed}")]
    private static partial void LogBatchRunFinalized(ILogger logger, Guid batchRunId, int fetched, int upserted, int downloaded, int failed);
}
