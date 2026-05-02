using ComiCal.Application.UseCases.User;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

public sealed class UserResolutionMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly Action<ILogger, string, Exception?> s_failedToResolveUser =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1001, "FailedToResolveUser"),
            "Failed to resolve user {Subject}");

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Items.TryGetValue("SwaClientPrincipal", out var obj) && obj is SwaClientPrincipal cp
            && !string.IsNullOrEmpty(cp.IdentityProvider)
            && !string.IsNullOrEmpty(cp.UserId))
        {
            try
            {
                var useCase = context.InstanceServices.GetRequiredService<ResolveUserUseCase>();
                var result = await useCase.ExecuteAsync(
                    cp.IdentityProvider, cp.UserId, cp.UserDetails ?? cp.UserId);
                if (result.IsSuccess)
                    context.Items["ResolvedUser"] = result.Value;
            }
            catch (Exception ex)
            {
                var logger = context.InstanceServices.GetRequiredService<ILogger<UserResolutionMiddleware>>();
                s_failedToResolveUser(logger, cp.UserId, ex);
            }
        }
        await next(context);
    }
}
