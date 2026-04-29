using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

namespace ComiCal.Infrastructure.Rakuten;

/// <summary>
/// <see cref="IRakutenBooksClient"/> を typed HttpClient + Polly resilience pipeline +
/// <see cref="RakutenRateLimitingHandler"/>（1 req/sec）で登録する DI 拡張。
/// </summary>
public static class RakutenBooksClientExtensions
{
    /// <summary>楽天 Books API クライアントを登録する。</summary>
    public static IServiceCollection AddRakutenBooksClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RakutenBooksOptions>()
            .Bind(configuration.GetSection(RakutenBooksOptions.SectionName))
            .ValidateOnStart();

        // RateLimiter handler must be a transient (per HttpClient) but the underlying
        // RateLimiter should be a singleton so all callers share the same bucket.
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RakutenBooksOptions>>().Value;
            return new RakutenRateLimitingHandler(opts.RatePerSecond);
        });

        services.AddHttpClient<IRakutenBooksClient, RakutenBooksClient>(RakutenBooksClient.HttpClientName,
            (sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RakutenBooksOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .AddHttpMessageHandler(sp => sp.GetRequiredService<RakutenRateLimitingHandler>())
            .AddResilienceHandler("rakuten-books-resilience", static builder =>
            {
                // Retry: 3 attempts, exponential backoff, on 5xx / 429 / transient HttpRequestException.
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(static r => (int)r.StatusCode >= 500
                            || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        .Handle<HttpRequestException>(),
                });
                // Circuit breaker on consecutive failures.
                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(static r => (int)r.StatusCode >= 500
                            || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        .Handle<HttpRequestException>(),
                });
                // Per-attempt timeout.
                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        return services;
    }
}
