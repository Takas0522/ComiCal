using ComiCal.Api.Middleware;
using ComiCal.Application;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseMiddleware<CorrelationMiddleware>();
        builder.UseMiddleware<SwaAuthMiddleware>();
        builder.UseMiddleware<UserResolutionMiddleware>();
        builder.UseMiddleware<RateLimitMiddleware>();
        builder.UseMiddleware<ProblemDetailsMiddleware>();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddApplicationServices();

        var connectionString = ctx.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString is required");
        services.AddSqlInfrastructure(connectionString);

        var storageUri = ctx.Configuration["StorageAccountUri"]
            ?? throw new InvalidOperationException("StorageAccountUri is required");
        services.AddBlobInfrastructure(storageUri);

        services.AddSingleton<RateLimitMiddleware.Limiter>();

        services.AddSingleton<BlobBaseUrl>(_ =>
            new BlobBaseUrl(ctx.Configuration["BlobBaseUrl"] ?? storageUri + "/covers"));
    })
    .Build();

await host.RunAsync();
