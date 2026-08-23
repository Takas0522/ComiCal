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
                // Azure SQL Serverless の auto-resume（60〜120 秒程度）を確実に吸収するため
                // リトライ回数と最大遅延を拡張する。デフォルトの EnableRetryOnFailure(3) では
                // 累積待機が約 10 秒に留まり、error 40613 (Database is not currently available)
                // を突き抜けて失敗する事象が本番で確認されたため対策する。
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 8,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
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
