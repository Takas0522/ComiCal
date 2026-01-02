using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ComiCal.Batch.Functions
{
    public static class HealthCheck
    {
        [Function("HealthCheck")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("HealthCheck");
            log.LogInformation("HealthCheck function called");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain");
            
            await response.WriteStringAsync($"ComiCal.Batch is running at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            return response;
        }
    }
}