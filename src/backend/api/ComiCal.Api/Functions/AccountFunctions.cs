using ComiCal.Api.Extensions;
using ComiCal.Application.UseCases.Account;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ComiCal.Api.Functions;

public static class AccountFunctions
{
    [Function("DeleteAccount")]
    public static async Task<HttpResponseData> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/me/account/delete")] HttpRequestData req,
        FunctionContext ctx,
        CancellationToken ct)
    {
        if (!ctx.TryGetResolvedUser(out var user))
            return await req.ToProblemAsync(ComiCal.Shared.Error.Unauthorized());

        var useCase = ctx.InstanceServices.GetRequiredService<DeleteAccountUseCase>();
        var result = await useCase.ExecuteAsync(user.UserId, ct);
        if (result.IsFailure) return await req.ToProblemAsync(result.Error);

        return req.CreateResponse(HttpStatusCode.Accepted);
    }
}
