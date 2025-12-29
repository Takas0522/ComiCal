using System.Text.Json;
using ComiCal.Batch.Repositories;
using ComiCal.Batch.Services;
using ComiCal.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Shared configuration from ComiCal.Shared
        services.AddComicalStartupSharedConfiguration(context.Configuration);

        // Register Batch-specific services
        services.AddHttpClient();
        services.AddSingleton<IRakutenComicRepository, RakutenComicRepository>();
        services.AddSingleton<IComicRepository, ComicRepository>();
        services.AddSingleton<IComicService, ComicService>();

        // Configure JSON serializer options
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
        });
    })
    .Build();

host.Run();
