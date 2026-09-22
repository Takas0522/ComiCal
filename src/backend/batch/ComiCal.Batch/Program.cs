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
        // Batch はユーザーに直接見えない非同期処理のため、Serverless の auto-resume
        // （60〜120 秒程度）を確実に吸収できるよう長めの ConnectTimeout / リトライを使う。
        // WarmupBatchTimer による事前 resume が効いていれば通常はここまで待たない。
        services.AddSqlInfrastructure(connectionString, SqlInfrastructureOptions.BatchDefaults);

        var storageUri = ctx.Configuration["StorageAccountUri"]
            ?? throw new InvalidOperationException("StorageAccountUri is required");
        services.AddBlobInfrastructure(storageUri);

        var rakutenAppId = ctx.Configuration["RakutenApplicationId"]
            ?? throw new InvalidOperationException("RakutenApplicationId is required");
        var rakutenAccessKey = ctx.Configuration["RakutenAccessKey"]
            ?? throw new InvalidOperationException("RakutenAccessKey is required");
        var rakutenAffiliateId = ctx.Configuration["RakutenAffiliateId"]
            ?? throw new InvalidOperationException("RakutenAffiliateId is required");
        services.AddRakutenInfrastructure(rakutenAppId, rakutenAccessKey, rakutenAffiliateId);

        // Durable Task orchestrators/activities are auto-discovered by
        // Microsoft.Azure.Functions.Worker.Extensions.DurableTask — no manual registration needed.
    })
    .Build();

await host.RunAsync();
