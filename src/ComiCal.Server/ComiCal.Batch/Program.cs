using System.Text.Json;
using ComiCal.Batch.Repositories;
using ComiCal.Batch.Services;
using ComiCal.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
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

        // Configure JSON serializer options
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    })
    .Build();

host.Run();
