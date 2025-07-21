using Comical.Api.Repositories;
using Comical.Api.Services;
using ComiCal.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var config = context.Configuration;
        services.AddComicalStartupSharedConfiguration(config);

        services.AddSingleton<IComicRepository, ComicRepository>();
        services.AddSingleton<IComicService, ComicService>();
        services.AddSingleton<IConfigMigrationRepository, ConfigMigrationRepository>();
        services.AddSingleton<IConfigMigrationService, ConfigMigrationService>();
    })
    .Build();

host.Run();