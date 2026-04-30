using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure.Sql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ComiCalDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(30);
            }));

        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IVolumeRepository, VolumeRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IThumbnailAssetRepository, ThumbnailAssetRepository>();
        services.AddScoped<IBatchRunRepository, BatchRunRepository>();

        return services;
    }
}
