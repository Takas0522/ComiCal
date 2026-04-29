using Azure.Identity;
using ComiCal.Api.Common;
using ComiCal.Api.Middleware;
using ComiCal.Api.Observability;
using ComiCal.Api.ProblemDetails;
using ComiCal.Application;
using ComiCal.Application.UseCases;
using ComiCal.Application.UseCases.Me;
using ComiCal.Infrastructure;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Azure App Configuration is only wired in deployed environments where the
// endpoint is provisioned (see infra/modules/app.bicep). Locally the
// Functions host falls back to the `FeatureManagement` section of
// local.settings.json, so the feature manager works without any Azure call.
var appConfigEndpoint = builder.Configuration["APP_CONFIGURATION_ENDPOINT"];
if (!string.IsNullOrWhiteSpace(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options
            .Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
            .UseFeatureFlags(ff => ff.SetRefreshInterval(TimeSpan.FromSeconds(30)));
    });
}

// 5-stage middleware order per docs/specs/oo-init/12-backend-api.md §12.5
builder.UseMiddleware<CorrelationMiddleware>();
builder.UseMiddleware<CorrelationLogScopeMiddleware>();
builder.UseMiddleware<SwaAuthMiddleware>();
builder.UseMiddleware<CurrentUserResolverMiddleware>();
builder.UseMiddleware<RateLimitMiddleware>();
builder.UseMiddleware<ValidationMiddleware>();
builder.UseMiddleware<ProblemDetailsMiddleware>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// docs/specs/oo-init/14-observability-sre.md §14.3 — stamp every telemetry item with
// `cloud_RoleName = "comical-api"` so the Application Map distinguishes the API and
// Batch Function Apps. Singleton because TelemetryInitializers are invoked from the
// SDK's own pipeline; per-request user id is resolved via a created scope inside.
builder.Services.AddSingleton<ITelemetryInitializer, CloudRoleNameInitializer>();

// The Functions Isolated host installs a default warning-level filter on the
// ApplicationInsightsLoggerProvider that swallows our Information-level structured logs.
// Removing the rule lets the global "default: Information" rule from host.json win.
// (See https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide#application-insights .)
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

builder.Services.AddFeatureManagement();

builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

// Phase 1 use cases (anonymous browse / search). IGetHealthUseCase is registered by
// AddComiCalInfrastructure for symmetry with prior wiring; everything else lives here
// so the api project owns its own UseCase composition root.
builder.Services.AddScoped<ISearchSeriesUseCase, SearchSeriesUseCase>();
builder.Services.AddScoped<IGetSeriesDetailUseCase, GetSeriesDetailUseCase>();
builder.Services.AddScoped<ISearchVolumesUseCase, SearchVolumesUseCase>();
builder.Services.AddScoped<IGetVolumeByIsbnUseCase, GetVolumeByIsbnUseCase>();
builder.Services.AddScoped<IGetCalendarUseCase, GetCalendarUseCase>();

// Phase 2: authenticated /api/me/* use cases.
builder.Services.AddScoped<IListSubscriptionsUseCase, ListSubscriptionsUseCase>();
builder.Services.AddScoped<IAddSubscriptionUseCase, AddSubscriptionUseCase>();
builder.Services.AddScoped<IRemoveSubscriptionUseCase, RemoveSubscriptionUseCase>();
builder.Services.AddScoped<IListPurchasesUseCase, ListPurchasesUseCase>();
builder.Services.AddScoped<IAddPurchaseUseCase, AddPurchaseUseCase>();
builder.Services.AddScoped<IRemovePurchaseUseCase, RemovePurchaseUseCase>();
builder.Services.AddScoped<IMergeAnonymousDataUseCase, MergeAnonymousDataUseCase>();
builder.Services.AddScoped<IIssueSyncTokenUseCase, IssueSyncTokenUseCase>();
builder.Services.AddScoped<IRedeemSyncTokenUseCase, RedeemSyncTokenUseCase>();
builder.Services.AddScoped<IDeleteAccountUseCase, DeleteAccountUseCase>();

builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());

builder.Build().Run();
