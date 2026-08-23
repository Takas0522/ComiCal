using ComiCal.Infrastructure.Sql;
using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ComiCal.Api.Middleware;

public sealed class ProblemDetailsMiddleware(ILogger<ProblemDetailsMiddleware> logger) : IFunctionsWorkerMiddleware
{
    /// <summary>
    /// Azure SQL Serverless の auto-resume 中クライアントに待たせる目安秒数。
    /// フロントの retry.interceptor はこの値を Retry-After ヘッダーから読み取り、再試行する。
    /// </summary>
    private const int TransientRetryAfterSeconds = 30;

    private static readonly Action<ILogger, string, Exception?> s_unhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1002, "UnhandledException"),
            "Unhandled exception in function {FunctionName}");

    private static readonly Action<ILogger, string, Exception?> s_transientSqlException =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1003, "TransientSqlException"),
            "Transient SQL exception in function {FunctionName}; returning 503 with Retry-After");

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var req = await context.GetHttpRequestDataAsync();
            if (req is null)
            {
                s_unhandledException(logger, context.FunctionDefinition.Name, ex);
                return;
            }

            var traceId = context.Items.TryGetValue("TraceId", out var t) ? t?.ToString() : null;

            if (SqlTransientErrorClassifier.IsTransient(ex))
            {
                s_transientSqlException(logger, context.FunctionDefinition.Name, ex);
                var res503 = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                res503.Headers.Add("Retry-After", TransientRetryAfterSeconds.ToString(CultureInfo.InvariantCulture));
                await res503.WriteAsJsonAsync(new
                {
                    type = "https://comical.example.jp/errors/service-unavailable",
                    title = "Database is temporarily unavailable. Please retry shortly.",
                    status = 503,
                    traceId
                }, "application/problem+json");
                context.GetInvocationResult().Value = res503;
                return;
            }

            s_unhandledException(logger, context.FunctionDefinition.Name, ex);
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

