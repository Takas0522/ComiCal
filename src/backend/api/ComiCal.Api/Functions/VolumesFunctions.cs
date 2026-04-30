using ComiCal.Api.Extensions;
using ComiCal.Api.Middleware;
using ComiCal.Application.UseCases.Volumes;
using ComiCal.Domain.Queries;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class VolumesFunctions
{
    [Function("GetUpcomingVolumes")]
    public static async Task<HttpResponseData> GetUpcomingAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/volumes/upcoming")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var useCase = ctx.InstanceServices.GetRequiredService<GetUpcomingVolumesUseCase>();
        var blobUrl = ctx.InstanceServices.GetRequiredService<BlobBaseUrl>().Value;

        _ = int.TryParse(req.GetQueryParam("limit"), out var limit);
        var query = new UpcomingQuery(req.GetQueryParam("cursor"), limit > 0 ? limit : 30);

        var result = await useCase.ExecuteAsync(query, blobUrl, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }

    [Function("GetCalendarVolumes")]
    public static async Task<HttpResponseData> GetCalendarAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/volumes/calendar")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var useCase = ctx.InstanceServices.GetRequiredService<GetCalendarVolumesUseCase>();
        var blobUrl = ctx.InstanceServices.GetRequiredService<BlobBaseUrl>().Value;

        if (!int.TryParse(req.GetQueryParam("year"), out var year))
        {
            var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            bad.Headers.Add("Content-Type", "application/problem+json");
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "year is required",
                status = 400
            }, ct);
            return bad;
        }

        _ = int.TryParse(req.GetQueryParam("month"), out var month);
        _ = int.TryParse(req.GetQueryParam("week"), out var week);
        var query = new CalendarQuery(year, month > 0 ? month : 1, week > 0 ? week : null);

        var result = await useCase.ExecuteAsync(query, blobUrl, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }
}
