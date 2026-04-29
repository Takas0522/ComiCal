using System.Threading.RateLimiting;
using ComiCal.Api.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace ComiCal.Api.Middleware;

/// <summary>
/// In-process token-bucket rate limiter, partitioned by remote IP. Phase 1 hosts no
/// authenticated traffic, so the partition key is derived from the SWA-forwarded
/// <c>X-Forwarded-For</c> header (falling back to the socket address). When a caller
/// exhausts their budget a <see cref="TooManyRequestsException"/> is thrown and
/// converted to a 429 RFC 7807 response by <see cref="ProblemDetailsMiddleware"/>.
/// Replenishment / window: 100 tokens per partition, refilled fully every 60 s
/// (≈ 100 req/min/IP).
/// </summary>
public sealed class RateLimitMiddleware : IFunctionsWorkerMiddleware, IDisposable
{
    private const int TokenLimit = 100;
    private static readonly TimeSpan ReplenishmentPeriod = TimeSpan.FromMinutes(1);

    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly PartitionedRateLimiter<string> _limiter;

    public RateLimitMiddleware(ILogger<RateLimitMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _limiter = PartitionedRateLimiter.Create<string, string>(static partitionKey =>
            RateLimitPartition.GetTokenBucketLimiter(
                partitionKey,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = TokenLimit,
                    TokensPerPeriod = TokenLimit,
                    ReplenishmentPeriod = ReplenishmentPeriod,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true,
                }));
    }

    public async System.Threading.Tasks.Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var partitionKey = ResolvePartitionKey(context);
        using var lease = await _limiter.AcquireAsync(partitionKey, permitCount: 1, context.CancellationToken).ConfigureAwait(false);

        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retry)
                ? (int)Math.Ceiling(retry.TotalSeconds)
                : (int)ReplenishmentPeriod.TotalSeconds;
            _logger.LogWarning(
                "Rate limit exceeded for partition {Partition}; retry after {Seconds}s",
                partitionKey,
                retryAfter);
            throw new TooManyRequestsException(retryAfter);
        }

        await next(context).ConfigureAwait(false);
    }

    public void Dispose() => _limiter.Dispose();

    private static string ResolvePartitionKey(FunctionContext context)
    {
        var http = context.GetHttpContext();
        if (http is null)
        {
            return "non-http";
        }

        if (http.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && forwarded.Count > 0)
        {
            var first = forwarded[0];
            if (!string.IsNullOrWhiteSpace(first))
            {
                var commaIdx = first.IndexOf(',');
                return (commaIdx < 0 ? first : first[..commaIdx]).Trim();
            }
        }

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
