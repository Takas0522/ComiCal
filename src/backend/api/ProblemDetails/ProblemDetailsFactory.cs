using System.Collections.Generic;
using System.Linq;
using ComiCal.Shared;
using FluentValidation.Results;

namespace ComiCal.Api.ProblemDetails;

/// <summary>
/// Builds <see cref="ProblemDetailsBody"/> instances from domain errors, validation
/// failures, or generic exception conditions. Centralises the <c>type</c> URI scheme
/// (<c>https://comical.example.com/problems/{slug}</c>) used across the Phase 1 API.
/// </summary>
public sealed class ProblemDetailsFactory
{
    private const string TypeBase = "https://comical.example.com/problems/";

    /// <summary>RFC 7807 problem from a use case <see cref="Error"/>.</summary>
    public ProblemDetailsBody FromError(Error error, string? instance, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (status, slug, title) = error.Kind switch
        {
            ErrorKind.Validation => (400, error.Code, "Validation failed"),
            ErrorKind.Unauthorized => (401, "unauthorized", "Unauthorized"),
            ErrorKind.Forbidden => (403, "forbidden", "Forbidden"),
            ErrorKind.NotFound => (404, error.Code, "Not found"),
            ErrorKind.Conflict => (409, error.Code, "Conflict"),
            ErrorKind.RateLimited => (429, "rate-limited", "Too many requests"),
            _ => (500, "internal-error", "Internal server error"),
        };

        return new ProblemDetailsBody(
            Type: TypeBase + slug,
            Title: title,
            Status: status,
            Detail: error.Message,
            Instance: instance,
            TraceId: traceId,
            Errors: null);
    }

    /// <summary>RFC 7807 problem from FluentValidation failures (HTTP 400).</summary>
    public ProblemDetailsBody FromValidation(IReadOnlyCollection<ValidationFailure> failures, string? instance, string? traceId)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var grouped = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => string.IsNullOrEmpty(g.Key) ? "_" : g.Key,
                g => (IReadOnlyList<string>)g.Select(f => f.ErrorMessage).ToList());

        return new ProblemDetailsBody(
            Type: TypeBase + "validation",
            Title: "Validation failed",
            Status: 400,
            Detail: "One or more request parameters were invalid.",
            Instance: instance,
            TraceId: traceId,
            Errors: grouped);
    }

    /// <summary>RFC 7807 problem for HTTP 429 with rate-limit metadata.</summary>
    public ProblemDetailsBody RateLimited(string? instance, string? traceId)
        => new(
            Type: TypeBase + "rate-limited",
            Title: "Too many requests",
            Status: 429,
            Detail: "The request was rate-limited. Retry after the time advertised in the Retry-After header.",
            Instance: instance,
            TraceId: traceId,
            Errors: null);

    /// <summary>RFC 7807 problem for an unhandled exception (HTTP 500). Detail is omitted in non-Development environments to avoid leaking implementation details.</summary>
    public ProblemDetailsBody Unhandled(string? detail, string? instance, string? traceId)
        => new(
            Type: TypeBase + "internal-error",
            Title: "Internal server error",
            Status: 500,
            Detail: detail,
            Instance: instance,
            TraceId: traceId,
            Errors: null);
}
