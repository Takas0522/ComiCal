using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// Extracts (or generates) a correlation id and exposes it on the
/// <see cref="FunctionContext"/> for downstream middleware and Function handlers.
/// The id is propagated back to the client via the <c>X-Correlation-Id</c>
/// response header so it can be referenced in support tickets.
/// </summary>
public sealed class CorrelationMiddleware(ILogger<CorrelationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    /// <summary>Header name used for inbound and outbound correlation ids.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Key used to stash the correlation id on <see cref="FunctionContext.Items"/>.</summary>
    public const string ItemKey = "__comical.correlationId";

    private readonly ILogger<CorrelationMiddleware> _logger = logger;

    public async System.Threading.Tasks.Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemKey] = correlationId;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["InvocationId"] = context.InvocationId,
        });

        await next(context).ConfigureAwait(false);

        var http = context.GetHttpContext();
        if (http is not null && !http.Response.HasStarted)
        {
            http.Response.Headers[HeaderName] = correlationId;
        }
    }

    private static string ResolveCorrelationId(FunctionContext context)
    {
        var http = context.GetHttpContext();
        if (http is not null
            && http.Request.Headers.TryGetValue(HeaderName, out var values)
            && values.Count > 0
            && !string.IsNullOrWhiteSpace(values[0]))
        {
            return values[0]!;
        }

        return Activity.Current?.TraceId.ToString() ?? context.InvocationId;
    }
}

/// <summary>Helpers to read context items written by <see cref="CorrelationMiddleware"/>.</summary>
internal static class CorrelationContextAccessor
{
    public static string? GetCorrelationId(FunctionContext context)
    {
        return context.Items.TryGetValue(CorrelationMiddleware.ItemKey, out var v) ? v as string : null;
    }
}
