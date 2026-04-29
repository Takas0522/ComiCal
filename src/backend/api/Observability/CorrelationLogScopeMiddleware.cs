using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Observability;

/// <summary>
/// Pushes a <c>correlationId</c> log scope around every Function invocation so structured
/// logs carry the W3C trace id as a dedicated field (separate from the human-friendly
/// <c>X-Correlation-Id</c> handled by <see cref="ComiCal.Api.Middleware.CorrelationMiddleware"/>).
/// Scope properties are exposed as <c>customDimensions</c> in Application Insights.
/// </summary>
/// <remarks>
/// Registered via <c>builder.UseMiddleware&lt;CorrelationLogScopeMiddleware&gt;()</c>.
/// Listed after <see cref="ComiCal.Api.Middleware.CorrelationMiddleware"/> so the scope picks up
/// the same id the response header advertises.
/// </remarks>
public sealed class CorrelationLogScopeMiddleware(ILoggerFactory loggerFactory) : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("ComiCal.Api.Correlation");

    /// <inheritdoc />
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var correlationId = Activity.Current?.Id
            ?? Activity.Current?.TraceId.ToString()
            ?? context.InvocationId;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["functionName"] = context.FunctionDefinition.Name,
        }))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
