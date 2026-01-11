using Comical.Api.Repositories;
using Comical.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IComicRepository, ComicRepository>();
builder.Services.AddSingleton<IConfigMigrationRepository, ConfigMigrationRepository>();
builder.Services.AddSingleton<IComicService, ComicService>();
builder.Services.AddSingleton<IConfigMigrationService, ConfigMigrationService>();

builder.Build().Run();
