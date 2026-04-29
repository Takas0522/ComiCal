using System.Text.Json;
using ComiCal.Batch.Models;
using ComiCal.Batch.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Triggers;

/// <summary>
/// Admin-only HTTP trigger that schedules a new <see cref="DailyReleaseOrchestrator"/> on demand.
/// Function-key authorization; intended to be called from Admin tools only.
/// </summary>
public sealed class ManualTriggerFunction(ILogger<ManualTriggerFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<ManualTriggerFunction> _logger = logger;

    [Function("ManualBatchTrigger")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "batch/runs")] HttpRequestData request,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executionContext);

        var ct = executionContext.CancellationToken;

        ManualTriggerRequest? body = null;
        if (request.Body.CanRead && request.Body.Length > 0)
        {
            body = await JsonSerializer
                .DeserializeAsync<ManualTriggerRequest>(request.Body, JsonOptions, ct)
                .ConfigureAwait(false);
        }

        var keyword = string.IsNullOrWhiteSpace(body?.Keyword) ? "コミック" : body!.Keyword!;
        var input = new BatchRunInput(Keyword: keyword, MaxPages: 10, RunId: Guid.CreateVersion7());

        var instanceId = await client
            .ScheduleNewOrchestrationInstanceAsync(nameof(DailyReleaseOrchestrator), input, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "ManualBatchTrigger scheduled orchestration {InstanceId} (Keyword={Keyword}, DryRun={DryRun})",
            instanceId,
            keyword,
            body?.DryRun ?? false);

        return await client.CreateCheckStatusResponseAsync(request, instanceId, ct).ConfigureAwait(false);
    }
}
