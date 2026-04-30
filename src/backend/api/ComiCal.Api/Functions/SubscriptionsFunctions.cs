using ComiCal.Api.Extensions;
using ComiCal.Application.UseCases.Subscriptions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class SubscriptionsFunctions
{
    [Function("GetSubscriptions")]
    public static async Task<HttpResponseData> GetAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "v1/me/subscriptions")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var useCase = ctx.InstanceServices.GetRequiredService<GetSubscriptionsUseCase>();
        var result = await useCase.ExecuteAsync(user.UserId, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }

    [Function("AddSubscription")]
    public static async Task<HttpResponseData> AddAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/me/subscriptions")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var body = await req.ReadJsonAsync<AddSubscriptionBody>();
        if (body is null || body.SeriesId == Guid.Empty)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            bad.Headers.Add("Content-Type", "application/problem+json");
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "seriesId is required",
                status = 400
            }, ct);
            return bad;
        }

        var useCase = ctx.InstanceServices.GetRequiredService<AddSubscriptionUseCase>();
        var result = await useCase.ExecuteAsync(user.UserId, body.SeriesId, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(result.Value, ct);
        return res;
    }

    [Function("RemoveSubscription")]
    public static async Task<HttpResponseData> RemoveAsync(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "v1/me/subscriptions/{seriesId}")] HttpRequestData req,
        Guid seriesId,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var useCase = ctx.InstanceServices.GetRequiredService<RemoveSubscriptionUseCase>();
        var result = await useCase.ExecuteAsync(user.UserId, seriesId, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private sealed record AddSubscriptionBody(Guid SeriesId);
}
