using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Triggers;

public partial class BatchTriggers(ILogger<BatchTriggers> logger)
{
    // Schedule is driven by the "DailyBatchCronExpression" app setting (NCRONTAB, UTC) so that
    // dev/prod can run at different times (e.g. offset by a few hours to avoid both environments
    // hitting the shared Rakuten API credentials at the same time) purely via Azure app settings,
    // without a code change/redeploy. Default (see infra/modules/app.bicep) is 18:00 UTC = 03:00 JST.
    [Function("DailyBatchTimer")]
    public async Task RunTimerAsync(
        [TimerTrigger("%DailyBatchCronExpression%")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        LogTimerTriggered(logger, timerInfo.IsPastDue);
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            "DailyFetchOrchestrator", ct);
        LogTimerInstanceStarted(logger, instanceId);
    }

    [Function("TriggerBatchHttp")]
    public async Task<string> RunHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "batch/trigger")] object req,
        [DurableClient] DurableTaskClient client,
        CancellationToken ct)
    {
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            "DailyFetchOrchestrator", ct);
        LogHttpTriggerStarted(logger, instanceId);
        return instanceId;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Daily batch timer triggered. IsPastDue: {IsPastDue}")]
    private static partial void LogTimerTriggered(ILogger logger, bool isPastDue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started DailyFetchOrchestrator with instance {InstanceId}")]
    private static partial void LogTimerInstanceStarted(ILogger logger, string instanceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Manual batch trigger started instance {InstanceId}")]
    private static partial void LogHttpTriggerStarted(ILogger logger, string instanceId);
}
