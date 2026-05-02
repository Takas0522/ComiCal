using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ComiCal.Api.Middleware;

public sealed class ProblemDetailsMiddleware(ILogger<ProblemDetailsMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private static readonly Action<ILogger, string, Exception?> s_unhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1002, "UnhandledException"),
            "Unhandled exception in function {FunctionName}");

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            s_unhandledException(logger, context.FunctionDefinition.Name, ex);
            var req = await context.GetHttpRequestDataAsync();
            if (req is not null)
            {
                var traceId = context.Items.TryGetValue("TraceId", out var t) ? t?.ToString() : null;
                var res = req.CreateResponse(HttpStatusCode.InternalServerError);
                // NOTE: WriteAsJsonAsync overload below sets Content-Type to "application/problem+json"
                //       internally. Adding the header manually causes a duplicate-value FormatException.
                await res.WriteAsJsonAsync(new
                {
                    type = "https://comical.example.jp/errors/internal",
                    title = "An unexpected error occurred",
                    status = 500,
                    traceId
                }, "application/problem+json");
                context.GetInvocationResult().Value = res;
            }
        }
    }
}
