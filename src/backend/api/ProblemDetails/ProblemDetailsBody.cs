using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ComiCal.Api.ProblemDetails;

/// <summary>
/// RFC 7807 Problem Details payload. Serialised as <c>application/problem+json</c>.
/// Used as the OpenAPI response body type for all non-success endpoints.
/// </summary>
public sealed record ProblemDetailsBody(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("instance")] string? Instance,
    [property: JsonPropertyName("traceId")] string? TraceId,
    [property: JsonPropertyName("errors")] IReadOnlyDictionary<string, IReadOnlyList<string>>? Errors);
