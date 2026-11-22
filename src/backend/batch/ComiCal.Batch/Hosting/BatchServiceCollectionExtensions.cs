using ComiCal.Application;
using ComiCal.Batch.Activities;
using ComiCal.Batch.Jobs;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Batch.Hosting;

public static class BatchServiceCollectionExtensions
{
    public static IServiceCollection AddBatchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var insightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(insightsConnectionString))
            services.AddApplicationInsightsTelemetryWorkerService(options => options.ConnectionString = insightsConnectionString);

        services.AddApplicationServices();
        services.AddSqlInfrastructure(Required("SqlConnectionString"), SqlInfrastructureOptions.BatchDefaults);
        services.AddBlobInfrastructure(Required("StorageAccountUri"));
        services.AddRakutenInfrastructure(
            Required("RakutenApplicationId"), Required("RakutenAccessKey"), Required("RakutenAffiliateId"));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IActivityInvoker, ActivityInvoker>();
        services.AddScoped<CreateBatchRunActivity>();
        services.AddScoped<FetchPageActivity>();
        services.AddScoped<UpsertVolumesActivity>();
        services.AddScoped<DownloadThumbnailActivity>();
        services.AddScoped<FinalizeBatchRunActivity>();
        services.AddScoped<SqlWarmup>();
        services.AddSingleton<DailyFetchJob>();
        services.AddSingleton<FetchPagesJob>();
        services.AddSingleton<ThumbnailJob>();
        return services;

        string Required(string key) => configuration[key]
            ?? throw new InvalidOperationException($"{key} is required");
    }
}
