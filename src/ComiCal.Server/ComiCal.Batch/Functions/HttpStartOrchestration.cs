using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Functions
{
    public static class HttpStartOrchestration
    {
        [Function("HttpStartOrchestration")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orchestration/start")] HttpRequestData req,
            [DurableClient] DurableTaskClient starter,
            FunctionContext context)
        {
            var log = context.GetLogger("ComiCal.Batch.Functions.HttpStartOrchestration");

            // Safety: keep this endpoint local-only.
            var isRunningInAzure = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
            if (isRunningInAzure)
            {
                var forbidden = req.CreateResponse(HttpStatusCode.Forbidden);
                await forbidden.WriteStringAsync("This endpoint is intended for local development only.");
                return forbidden;
            }

            var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync("Orchestration");
            log.LogInformation("Started orchestration via HTTP. InstanceId={InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new { instanceId }));
            return response;
        }
    }
}
