using ComiCal.Api.Extensions;
using ComiCal.Api.Middleware;
using ComiCal.Application.UseCases.Series;
using ComiCal.Domain.Queries;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class SeriesFunctions
{
    [Function("SearchSeries")]
    public static async Task<HttpResponseData> SearchAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/series")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var useCase = ctx.InstanceServices.GetRequiredService<SearchSeriesUseCase>();
        var blobUrl = ctx.InstanceServices.GetRequiredService<BlobBaseUrl>().Value;

        DateOnly? releaseFrom = DateOnly.TryParse(req.GetQueryParam("releaseFrom"), out var rd) ? rd : null;
        _ = int.TryParse(req.GetQueryParam("limit"), out var limit);

        var query = new SeriesSearchQuery(
            req.GetQueryParam("q"),
            releaseFrom,
            req.GetQueryParam("publisher"),
            req.GetQueryParam("cursor"),
            limit > 0 ? limit : 20);

        var result = await useCase.ExecuteAsync(query, blobUrl, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }

    [Function("GetSeriesDetail")]
    public static async Task<HttpResponseData> GetDetailAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/series/{id}")] HttpRequestData req,
        Guid id,
        FunctionContext ctx,
        CancellationToken ct)
    {
        var useCase = ctx.InstanceServices.GetRequiredService<GetSeriesDetailUseCase>();
        var blobUrl = ctx.InstanceServices.GetRequiredService<BlobBaseUrl>().Value;

        var result = await useCase.ExecuteAsync(id, blobUrl, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }
}
