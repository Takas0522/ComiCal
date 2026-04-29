using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// Validation pass-through. The actual conversion of <see cref="ValidationException"/>
/// (raised by Function handlers via <c>IValidator&lt;T&gt;.ValidateAsync</c>) into
/// RFC 7807 responses happens in <see cref="ProblemDetailsMiddleware"/>, which is the
/// innermost middleware. This middleware is preserved for symmetry with the documented
/// 5-stage pipeline and as an extension point for future cross-cutting validation logic.
/// </summary>
public sealed class ValidationMiddleware(ILogger<ValidationMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger = logger;

    public async System.Threading.Tasks.Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        _logger.LogTrace("ValidationMiddleware invoked for {InvocationId}", context.InvocationId);
        await next(context).ConfigureAwait(false);
    }
}
