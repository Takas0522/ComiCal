using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;
using System.Threading.RateLimiting;

namespace ComiCal.Api.Middleware;

public sealed class RateLimitMiddleware(RateLimitMiddleware.Limiter limiter) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var key = GetKey(context);
        var isAuth = context.Items.ContainsKey("ResolvedUser");
        var lease = await limiter.AcquireAsync(key, isAuth);

        if (!lease.IsAcquired)
        {
            var req = await context.GetHttpRequestDataAsync();
            if (req is not null)
            {
                var res = req.CreateResponse(HttpStatusCode.TooManyRequests);
                res.Headers.Add("Retry-After", "60");
                await res.WriteAsJsonAsync(new
                {
                    type = "https://comical.example.jp/errors/rate-limit",
                    title = "Too Many Requests",
                    status = 429
                });
                context.GetInvocationResult().Value = res;
                return;
            }
        }
        await next(context);
    }

    private static string GetKey(FunctionContext context)
    {
        if (context.Items.TryGetValue("ResolvedUser", out var u) && u is ComiCal.Domain.Entities.User user)
            return $"user:{user.UserId}";
        return "anon:global";
    }

    public sealed class Limiter : IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, RateLimiter> _limiters = new();

        public ValueTask<RateLimitLease> AcquireAsync(string key, bool isAuthenticated)
        {
            var limiter = _limiters.GetOrAdd(key, _ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = isAuthenticated ? 60 : 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
            return limiter.AcquireAsync();
        }

        public void Dispose()
        {
            foreach (var l in _limiters.Values)
                l.Dispose();
        }
    }
}
