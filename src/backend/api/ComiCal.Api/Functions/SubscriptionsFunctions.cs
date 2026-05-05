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
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/me/subscriptions")] HttpRequestData req,
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/me/subscriptions")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var body = await req.ReadJsonAsync<AddSubscriptionBody>();
        if (body is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "seriesId または rakutenIsbn が必要です。",
                status = 400
            }, "application/problem+json", ct);
            return bad;
        }

        // 楽天 ISBN による購読（未登録シリーズをオンデマンドで取り込む）
        if (!string.IsNullOrWhiteSpace(body.RakutenIsbn))
        {
            var rakutenUseCase = ctx.InstanceServices.GetRequiredService<AddSubscriptionFromRakutenUseCase>();
            var result = await rakutenUseCase.ExecuteAsync(user.UserId, body.RakutenIsbn, ct);
            if (result.IsFailure) return await req.ToProblemAsync(result.Error);

            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(result.Value, ct);
            return res;
        }

        // 既存 DB シリーズによる購読
        if (body.SeriesId != Guid.Empty)
        {
            var useCase = ctx.InstanceServices.GetRequiredService<AddSubscriptionUseCase>();
            var result = await useCase.ExecuteAsync(user.UserId, body.SeriesId, ct);
            if (result.IsFailure) return await req.ToProblemAsync(result.Error);

            var res = req.CreateResponse(HttpStatusCode.Created);
            await res.WriteAsJsonAsync(result.Value, ct);
            return res;
        }

        var badRes = req.CreateResponse(HttpStatusCode.BadRequest);
        await badRes.WriteAsJsonAsync(new
        {
            type = "https://comical.example.jp/errors/validation",
            title = "seriesId または rakutenIsbn が必要です。",
            status = 400
        }, "application/problem+json", ct);
        return badRes;
    }

    [Function("RemoveSubscription")]
    public static async Task<HttpResponseData> RemoveAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "v1/me/subscriptions/{seriesId}")] HttpRequestData req,
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

    private sealed record AddSubscriptionBody(Guid SeriesId, string? RakutenIsbn);
}
