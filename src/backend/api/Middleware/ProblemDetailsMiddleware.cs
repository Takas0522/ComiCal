using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using ComiCal.Api.Common;
using ComiCal.Api.ProblemDetails;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// Outermost (after Correlation) middleware: catches unhandled exceptions and
/// converts them into RFC 7807 <c>application/problem+json</c> responses.
///
/// Mapping:
/// <list type="bullet">
///   <item><see cref="ValidationException"/> → 400 with field-level errors.</item>
///   <item><see cref="TooManyRequestsException"/> → 429 with <c>Retry-After</c>.</item>
///   <item>Anything else → 500 (detail suppressed outside Development).</item>
/// </list>
/// </summary>
public sealed class ProblemDetailsMiddleware(
    ILogger<ProblemDetailsMiddleware> logger,
    ProblemDetailsFactory factory,
    IHostEnvironment environment) : IFunctionsWorkerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<ProblemDetailsMiddleware> _logger = logger;
    private readonly ProblemDetailsFactory _factory = factory;
    private readonly IHostEnvironment _environment = environment;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(ex, "Validation failed for {InvocationId}", context.InvocationId);
            await WriteProblemAsync(
                context,
                _factory.FromValidation(ex.Errors.ToList(), instance: ResolveInstance(context), traceId: ResolveTraceId(context)),
                HttpStatusCode.BadRequest).ConfigureAwait(false);
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogWarning("Rate limit exceeded for {InvocationId}", context.InvocationId);
            await WriteProblemAsync(
                context,
                _factory.RateLimited(instance: ResolveInstance(context), traceId: ResolveTraceId(context)),
                HttpStatusCode.TooManyRequests,
                retryAfterSeconds: ex.RetryAfterSeconds).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {InvocationId}", context.InvocationId);
            var detail = _environment.IsDevelopment() ? ex.Message : null;
            await WriteProblemAsync(
                context,
                _factory.Unhandled(detail: detail, instance: ResolveInstance(context), traceId: ResolveTraceId(context)),
                HttpStatusCode.InternalServerError).ConfigureAwait(false);
        }
    }

    private static async Task WriteProblemAsync(
        FunctionContext context,
        ProblemDetailsBody body,
        HttpStatusCode status,
        int? retryAfterSeconds = null)
    {
        var http = context.GetHttpContext();
        if (http is null || http.Response.HasStarted)
        {
            return;
        }

        http.Response.StatusCode = (int)status;
        http.Response.ContentType = "application/problem+json";
        if (retryAfterSeconds is { } secs)
        {
            http.Response.Headers["Retry-After"] = secs.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await JsonSerializer.SerializeAsync(http.Response.Body, body, JsonOptions, http.RequestAborted).ConfigureAwait(false);
    }

    private static string? ResolveInstance(FunctionContext context)
    {
        var http = context.GetHttpContext();
        return http?.Request.Path.Value;
    }

    private static string? ResolveTraceId(FunctionContext context)
        => CorrelationContextAccessor.GetCorrelationId(context) ?? context.InvocationId;
}
