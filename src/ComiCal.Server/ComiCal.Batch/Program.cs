using System.Text.Json;
using ComiCal.Batch.Jobs;
using ComiCal.Batch.Repositories;
using ComiCal.Batch.Services;
using ComiCal.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        Console.WriteLine($"DEBUG: Environment = {environment}");
        
        // Clear existing configuration and rebuild
        config.Sources.Clear();
        
        // Add configuration sources in order
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
        
        // Build temporary config to verify values
        var tempConfig = config.Build();
        Console.WriteLine($"DEBUG: Config StorageConnectionString = '{tempConfig["StorageConnectionString"]}'");
    })
    .ConfigureServices((context, services) =>
    {
        // Shared configuration from ComiCal.Shared
        services.AddComicalStartupSharedConfiguration(context.Configuration);

        // Register Batch-specific services with connection pooling limits
        services.AddHttpClient<IRakutenComicRepository, RakutenComicRepository>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // Limit concurrent connections to prevent socket exhaustion
                MaxConnectionsPerServer = 2,
                // Keep connections alive for reuse
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                // Increase timeout for slow API responses
                ConnectTimeout = TimeSpan.FromSeconds(30)
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));
        
        services.AddSingleton<IComicRepository, ComicRepository>();
        services.AddSingleton<IComicService, ComicService>();

        // Register batch state management services
        services.AddSingleton<IBatchStateRepository, BatchStateRepository>();
        services.AddSingleton<IBatchStateService, BatchStateService>();
        services.AddSingleton<JobSchedulingService>();
        services.AddSingleton<PartialRetryService>();
        services.AddSingleton<IJobTriggerService, JobTriggerService>();

        // Register Container Jobs as BackgroundServices
        // Only the job specified by BATCH_JOB_TYPE environment variable will execute
        services.AddHostedService<RegistrationJob>();
        services.AddHostedService<ImageDownloadJob>();

        // Configure JSON serializer options
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    })
    .UseConsoleLifetime() // Ensure proper console app lifecycle for Container Apps Jobs
    .Build();

await host.RunAsync();
