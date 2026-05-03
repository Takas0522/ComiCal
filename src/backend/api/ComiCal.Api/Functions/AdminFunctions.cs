using ComiCal.Api.Extensions;
using ComiCal.Application.UseCases.Admin;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class AdminFunctions
{
    [Function("GetBatchRuns")]
    public static async Task<HttpResponseData> GetBatchRunsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/admin/batch-runs")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.IsAdmin())
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        _ = int.TryParse(req.GetQueryParam("limit"), out var limit);
        var useCase = ctx.InstanceServices.GetRequiredService<GetBatchRunsUseCase>();
        var result = await useCase.ExecuteAsync(req.GetQueryParam("cursor"), limit > 0 ? limit : 20, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }

    [Function("TriggerBatch")]
    public static async Task<HttpResponseData> TriggerBatchAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/admin/batch/trigger")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.IsAdmin())
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        // Batch trigger is handled by the Batch function app; return 202 Accepted
        var res = req.CreateResponse(HttpStatusCode.Accepted);
        await res.WriteAsJsonAsync(new { message = "Batch trigger accepted" }, ct);
        return res;
    }
}
