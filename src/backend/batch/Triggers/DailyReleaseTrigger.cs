using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Triggers;

/// <summary>
/// Daily timer trigger that schedules <see cref="DailyReleaseOrchestrator"/> at 03:00 JST (18:00 UTC).
/// </summary>
public sealed class DailyReleaseTrigger(ILogger<DailyReleaseTrigger> logger)
{
    private readonly ILogger<DailyReleaseTrigger> _logger = logger;

    [Function("DailyReleaseTrigger")]
    public async Task RunAsync(
        [TimerTrigger("0 0 18 * * *")] TimerInfo timer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executionContext);

        var input = new BatchRunInput(Keyword: "コミック", MaxPages: 10, RunId: Guid.CreateVersion7());

        var instanceId = await client
            .ScheduleNewOrchestrationInstanceAsync(
                nameof(DailyReleaseOrchestrator),
                input,
                executionContext.CancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "DailyReleaseTrigger scheduled orchestration {InstanceId} (RunId={RunId}, IsPastDue={IsPastDue})",
            instanceId,
            input.RunId,
            timer.IsPastDue);
    }
}
