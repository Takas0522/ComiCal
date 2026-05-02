using ComiCal.Application;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((ctx, services) =>
    {
        // Application Insights: only enable when connection string is configured
        if (!string.IsNullOrWhiteSpace(ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            services.AddApplicationInsightsTelemetryWorkerService();

        services.AddApplicationServices();

        var connectionString = ctx.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString is required");
        services.AddSqlInfrastructure(connectionString);

        var storageUri = ctx.Configuration["StorageAccountUri"]
            ?? throw new InvalidOperationException("StorageAccountUri is required");
        services.AddBlobInfrastructure(storageUri);

        var rakutenAppId = ctx.Configuration["RakutenApplicationId"]
            ?? throw new InvalidOperationException("RakutenApplicationId is required");
        services.AddRakutenInfrastructure(rakutenAppId);

        // Durable Task orchestrators/activities are auto-discovered by
        // Microsoft.Azure.Functions.Worker.Extensions.DurableTask — no manual registration needed.
    })
    .Build();

await host.RunAsync();
