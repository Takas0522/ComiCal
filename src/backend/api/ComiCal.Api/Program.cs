using ComiCal.Api.Middleware;
using ComiCal.Application;
using ComiCal.Infrastructure.Blob;
using ComiCal.Infrastructure.Rakuten;
using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Core.Serialization;

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
        // Configure HTTP / JSON serialization to camelCase so the Angular frontend
        // (whose openapi-generated types are camelCase) can deserialize responses.
        services.Configure<WorkerOptions>(o =>
        {
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            o.Serializer = new JsonObjectSerializer(json);
        });

        // Application Insights: only enable when connection string is configured
        // (func start injects an empty string by default, which causes a parse error)
        if (!string.IsNullOrWhiteSpace(ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            services.AddApplicationInsightsTelemetryWorkerService();

        services.AddApplicationServices();

        var connectionString = ctx.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString is required");
        services.AddSqlInfrastructure(connectionString);

        var storageUri = ctx.Configuration["StorageAccountUri"]
            ?? throw new InvalidOperationException("StorageAccountUri is required");
        services.AddBlobInfrastructure(storageUri);

        // 楽天 Books API（検索フォールバック用）。API キー未設定時は no-op 実装が登録される。
        var rakutenAppId = ctx.Configuration["RakutenApplicationId"];
        var rakutenAccessKey = ctx.Configuration["RakutenAccessKey"];
        var rakutenAffiliateId = ctx.Configuration["RakutenAffiliateId"];
        services.AddRakutenInfrastructure(rakutenAppId, rakutenAccessKey, rakutenAffiliateId);

        services.AddSingleton<RateLimitMiddleware.Limiter>();

        services.AddSingleton<BlobBaseUrl>(_ =>
            new BlobBaseUrl(ctx.Configuration["BlobBaseUrl"] ?? storageUri + "/covers"));
    })
    .Build();

await host.RunAsync();
