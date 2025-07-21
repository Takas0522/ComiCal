using ComiCal.Batch.Repositories;
using ComiCal.Batch.Services;
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

        services.AddHttpClient();
        services.AddSingleton<IRakutenComicRepository, RakutenComicRepository>();
        services.AddSingleton<IComicRepository, ComicRepository>();
        services.AddSingleton<IComicService, ComicService>();
        services.AddSingleton<IComicImageRepository, ComicImageRepository>();
    })
    .Build();

host.Run();