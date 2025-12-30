using Comical.Api.Repositories;
using Comical.Api.Services;
using ComiCal.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Shared configuration from ComiCal.Shared
        services.AddComicalStartupSharedConfiguration(context.Configuration);

        // Register API-specific services
        services.AddSingleton<IComicRepository, ComicRepository>();
        services.AddSingleton<IComicService, ComicService>();
        services.AddSingleton<IConfigMigrationRepository, ConfigMigrationRepository>();
        services.AddSingleton<IConfigMigrationService, ConfigMigrationService>();

        // Configure JSON serialization options globally
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    })
    .Build();

host.Run();
