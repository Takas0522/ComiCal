using ComiCal.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Threading.RateLimiting;

namespace ComiCal.Infrastructure.Rakuten;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRakutenInfrastructure(
        this IServiceCollection services, string applicationId)
    {
        // 1 request/second sliding window rate limiter
        var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10,
        });

        services.AddSingleton<RateLimiter>(_ => rateLimiter);

        services.AddHttpClient<RakutenBooksApiClient>(client =>
        {
            client.DefaultRequestHeaders.Add("X-Rakuten-AppId", applicationId);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
        });

        // シングルトンで登録（キャッシュを保持するため）
        services.AddSingleton<IRakutenBookSearchService, RakutenBookSearchService>();

        return services;
    }
}
