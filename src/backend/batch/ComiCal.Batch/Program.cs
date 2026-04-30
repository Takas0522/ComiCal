using ComiCal.Application;
using ComiCal.Batch.Activities;
using ComiCal.Batch.Orchestrators;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
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

        var rakutenAppId = ctx.Configuration["RakutenApplicationId"]
            ?? throw new InvalidOperationException("RakutenApplicationId is required");
        services.AddRakutenInfrastructure(rakutenAppId);

        services.AddDurableTaskWorker(builder =>
        {
            builder.AddTasks(r =>
            {
                r.AddOrchestrator<DailyFetchOrchestrator>();
                r.AddOrchestrator<FetchOrchestrator>();
                r.AddOrchestrator<ThumbnailOrchestrator>();
                r.AddActivity<CreateBatchRunActivity>();
                r.AddActivity<FetchPageActivity>();
                r.AddActivity<UpsertVolumesActivity>();
                r.AddActivity<DownloadThumbnailActivity>();
                r.AddActivity<FinalizeBatchRunActivity>();
            });
            builder.UseGrpc();
        });
    })
    .Build();

await host.RunAsync();
