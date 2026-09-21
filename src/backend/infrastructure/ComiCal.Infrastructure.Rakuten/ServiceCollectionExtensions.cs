using ComiCal.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRakutenInfrastructure(
        this IServiceCollection services, string? applicationId, string? accessKey = null, string? affiliateId = null)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            // API キー未設定の場合は no-op 実装を登録（検索候補が空になるが起動は成功する）
            services.AddSingleton<IRakutenBookSearchService, NullRakutenBookSearchService>();
            return services;
        }

        // Rakuten Books API rate limit is 1 request per 5 seconds (per the app's registered
        // "Expected QPS: 5" setting, which — confusingly — means "once every 5 seconds", not
        // "5 requests per second"). Enforced as a sliding window limiter.
        var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(5),
            SegmentsPerWindow = 5,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10,
        });

        services.AddSingleton<RateLimiter>(_ => rateLimiter);

        services.AddHttpClient<RakutenBooksApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        // NOTE: We do NOT use AddStandardResilienceHandler here because:
        // 1. We enforce a SlidingWindowRateLimiter (1 req/5sec) at the SearchComicsAsync level
        // 2. Adding automatic retries would bypass the rate limiter and cause 429 Too Many Requests
        // 3. The rate limiter is sufficient for handling transient failures gracefully

        // 認証情報をシングルトンで登録（キャッシュを保持するため）
        services.AddSingleton<RakutenAuthCredentials>(_ =>
            new RakutenAuthCredentials(applicationId, accessKey ?? string.Empty, affiliateId ?? string.Empty));

        // シングルトンで登録（キャッシュを保持するため）
        services.AddSingleton<IRakutenBookSearchService, RakutenBookSearchService>();

        return services;
    }
}
