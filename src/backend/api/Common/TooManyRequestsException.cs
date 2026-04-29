using System;

namespace ComiCal.Api.Common;

/// <summary>
/// Thrown by <see cref="Middleware.RateLimitMiddleware"/> when a caller exceeds the
/// configured token-bucket budget. <see cref="Middleware.ProblemDetailsMiddleware"/>
/// converts this into a 429 RFC 7807 response with a <c>Retry-After</c> header.
/// </summary>
public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException(int retryAfterSeconds)
        : base("Rate limit exceeded.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int RetryAfterSeconds { get; }
}
