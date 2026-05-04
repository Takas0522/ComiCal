using ComiCal.Api.Extensions;
using ComiCal.Application.UseCases.Purchases;
using ComiCal.Application.Validators;
using ComiCal.Domain.Enums;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class PurchasesFunctions
{
    [Function("UpdatePurchaseState")]
    public static async Task<HttpResponseData> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "v1/me/purchases/{volumeId}")] HttpRequestData req,
        Guid volumeId,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var body = await req.ReadJsonAsync<UpdatePurchaseStateRequest>();
        if (body is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "body is required",
                status = 400
            }, "application/problem+json", ct);
            return bad;
        }

        var validator = ctx.InstanceServices.GetRequiredService<IValidator<UpdatePurchaseStateRequest>>();
        var validation = await validator.ValidateAsync(body, ct);
        if (!validation.IsValid)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new
            {
                type = "https://comical.example.jp/errors/validation",
                title = "Validation failed",
                status = 400,
                errors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            }, "application/problem+json", ct);
            return bad;
        }

        var state = Enum.Parse<PurchaseState>(body.State);
        var useCase = ctx.InstanceServices.GetRequiredService<UpdatePurchaseStateUseCase>();
        var result = await useCase.ExecuteAsync(user.UserId, volumeId, state, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        return req.CreateResponse(HttpStatusCode.NoContent);
    }
}
