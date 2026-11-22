using ComiCal.Batch.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ComiCal.Batch.Hosting;

public interface IBatchWork
{
    Task WarmupAsync(CancellationToken ct);
    Task<Guid> FetchAsync(CancellationToken ct);
}

public sealed class BatchWork(IServiceScopeFactory scopes, IConfiguration configuration, Serilog.ILogger logger) : IBatchWork
{
    public async Task WarmupAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SqlWarmup>().RunAsync(ct);
    }

    public async Task<Guid> FetchAsync(CancellationToken ct)
    {
        // Rakuten and optional telemetry need the final configuration populated by Preflight.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Configuration.AddConfiguration(configuration);
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(config => config.WriteTo.Logger(logger), preserveStaticLogger: true, writeToProviders: true);
        builder.Services.AddBatchInfrastructure(builder.Configuration);
        using var host = builder.Build();

        await host.StartAsync(ct);
        try
        {
            return await host.Services.GetRequiredService<DailyFetchJob>().RunAsync(ct);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None);
        }
    }
}
