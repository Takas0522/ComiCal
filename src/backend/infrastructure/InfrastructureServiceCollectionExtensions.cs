using ComiCal.Application.UseCases;
using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.AppConfig;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Persistence;
using ComiCal.Infrastructure.Persistence.Interceptors;
using ComiCal.Infrastructure.Persistence.Repositories;
using ComiCal.Infrastructure.Rakuten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure;

/// <summary>Infrastructure 層を一括登録する DI 拡張。</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>EF Core / Rakuten / Blob / Feature flag を登録する。</summary>
    public static IServiceCollection AddComiCalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<AuditSaveChangesInterceptor>();

        var connectionString = configuration["SqlConnection"] ?? string.Empty;
        services.AddDbContext<ComiCalDbContext>((sp, options) =>
        {
            // Connection pooling: the SqlClient default (Pooling=True;Min=0;Max=100)
            // is appropriate for our workload. The Bicep-injected connection string
            // can override these via `Min Pool Size` / `Max Pool Size` if needed.
            // For Azure SQL Serverless auto-pause, set `Connection Lifetime=60` in
            // the connection string so stale pooled connections are recycled after
            // a resume event (see infra/modules/data.bicep).
            options
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3))
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddRakutenBooksClient(configuration);
        services.AddCoverBlobStorage(configuration);

        services.AddScoped<IGetHealthUseCase, GetHealthUseCase>();

        services.AddMemoryCache();
        services.AddSingleton<IFeatureFlagProvider, AppConfigFeatureFlagProvider>();

        return services;
    }

    /// <summary>ドメインリポジトリ実装を登録する。</summary>
    public static IServiceCollection AddComiCalRepositories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IVolumeRepository, VolumeRepository>();
        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<ISyncTokenRepository, SyncTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
