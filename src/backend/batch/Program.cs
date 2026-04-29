using ComiCal.Application;
using ComiCal.Batch.Activities;
using ComiCal.Batch.Observability;
using ComiCal.Infrastructure;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// docs/specs/oo-init/14-observability-sre.md §14.3 — distinguish batch telemetry from API.
builder.Services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();

// Custom metrics for the Daily Batch Workbook + alert rules
// (rakuten.api.calls, rakuten.api.rate_limited, batch.volumes_ingested, batch.duration_seconds).
builder.Services.AddSingleton<IBatchMetrics, BatchMetrics>();

// Functions Isolated installs a default warning-level filter on the AppInsights logger
// provider that drops Information-level structured logs. Removing it lets host.json win.
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    var rule = options.Rules.FirstOrDefault(r =>
        r.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (rule is not null)
    {
        options.Rules.Remove(rule);
    }
});

builder.Services.AddComiCalInfrastructure(builder.Configuration);
builder.Services.AddComiCalRepositories();

builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

// Cover image downloader: separate HttpClient with Polly retry; no rate limiter
// because the Rakuten image CDN is independent of the Books API quota.
builder.Services
    .AddHttpClient(EnsureCoverThumbnailActivity.CoverDownloaderClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddResilienceHandler("cover-downloader-resilience", static pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromMilliseconds(200),
        });
        pipeline.AddTimeout(TimeSpan.FromSeconds(15));
    });

builder.Build().Run();

