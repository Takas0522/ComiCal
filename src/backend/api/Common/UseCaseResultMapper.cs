using System.Threading.Tasks;
using ComiCal.Api.Middleware;
using ComiCal.Api.ProblemDetails;
using ComiCal.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ComiCal.Api.Common;

/// <summary>
/// Centralises the conversion of <see cref="Result{T}"/> into <see cref="IActionResult"/>
/// for Function HTTP triggers. Success paths optionally apply weak-ETag handling.
/// </summary>
internal static class UseCaseResultMapper
{
    /// <summary>Map a <see cref="Result{T}"/> to an HTTP response with optional ETag/304 support.</summary>
    /// <param name="cacheControl">Optional Cache-Control header for anonymous-safe
    /// reads (e.g. <c>"public, max-age=60, stale-while-revalidate=300"</c>). Never
    /// pass a value here for user-scoped responses (Me*).</param>
    public static async Task<IActionResult> ToActionResultAsync<T>(
        Result<T> result,
        HttpRequest request,
        FunctionContext context,
        ProblemDetailsFactory factory,
        bool useEtag,
        string? cacheControl = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);

        if (!result.IsSuccess)
        {
            return ToProblem(result.Error, request, context, factory);
        }

        if (useEtag)
        {
            return await EtagSupport.BuildResponseAsync(request, result.Value!, cacheControl).ConfigureAwait(false);
        }

        return new OkObjectResult(result.Value);
    }

    /// <summary>Map a domain <see cref="Error"/> to a problem+json ObjectResult.</summary>
    public static IActionResult ToProblem(
        Error error,
        HttpRequest request,
        FunctionContext context,
        ProblemDetailsFactory factory)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);

        var instance = request.Path.Value;
        var traceId = CorrelationContextAccessor.GetCorrelationId(context) ?? context.InvocationId;
        var body = factory.FromError(error, instance, traceId);

        var result = new ObjectResult(body)
        {
            StatusCode = body.Status,
            ContentTypes = { "application/problem+json" },
        };
        return result;
    }

    private static string? GetCorrelationId(FunctionContext context)
        => CorrelationContextAccessor.GetCorrelationId(context);
}
