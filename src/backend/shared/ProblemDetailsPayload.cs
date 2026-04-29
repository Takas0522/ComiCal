namespace ComiCal.Shared;

public sealed record ProblemDetailsPayload(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string? Instance,
    string? TraceId);

public static class ProblemDetailsMapper
{
    public static ProblemDetailsPayload FromError(Error error, string? instance = null, string? traceId = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        var status = error.Kind switch
        {
            ErrorKind.Validation => 400,
            ErrorKind.Unauthorized => 401,
            ErrorKind.Forbidden => 403,
            ErrorKind.NotFound => 404,
            ErrorKind.Conflict => 409,
            ErrorKind.RateLimited => 429,
            _ => 500,
        };
        return new ProblemDetailsPayload(
            Type: $"https://comical.example.com/errors/{error.Code}",
            Title: error.Kind.ToString(),
            Status: status,
            Detail: error.Message,
            Instance: instance,
            TraceId: traceId);
    }
}
