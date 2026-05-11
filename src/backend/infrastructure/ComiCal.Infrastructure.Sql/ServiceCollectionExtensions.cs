using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Sql.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure.Sql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        // Azure SQL Serverless の自動一時停止からの復旧に備え ConnectTimeout を延長する。
        // デフォルト 30 秒では auto-resume が間に合わず post-login タイムアウトが発生するため 90 秒に設定。
        var csb = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 90 };

        services.AddDbContext<ComiCalDbContext>(options =>
            options.UseSqlServer(csb.ConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(30);
            }));

        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IVolumeRepository, VolumeRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IThumbnailAssetRepository, ThumbnailAssetRepository>();
        services.AddScoped<IBatchRunRepository, BatchRunRepository>();

        return services;
    }
}
