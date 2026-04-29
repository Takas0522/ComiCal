using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// HttpClient pipeline に挟む <see cref="DelegatingHandler"/>。
/// <see cref="TokenBucketRateLimiter"/>（既定 1 token/sec, capacity 1）でリクエストを直列化する。
/// </summary>
internal sealed class RakutenRateLimitingHandler : DelegatingHandler, IDisposable
{
    private readonly RateLimiter _limiter;
    private bool _disposed;

    public RakutenRateLimitingHandler(int ratePerSecond)
    {
        if (ratePerSecond < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePerSecond), ratePerSecond, "rate must be >= 1");
        }
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = ratePerSecond,
            TokensPerPeriod = ratePerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = int.MaxValue,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(1, cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            throw new RakutenBooksApiException(
                "Failed to acquire rate-limit lease for Rakuten Books API call.",
                statusCode: null);
        }
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _limiter.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
