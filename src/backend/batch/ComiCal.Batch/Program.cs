using ComiCal.Batch.Hosting;
using ComiCal.Batch.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args is not ["run"])
{
    Console.Error.WriteLine("Usage: ComiCal.Batch run");
    return 1;
}

try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = AppContext.BaseDirectory
    });
    var localSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ComiCal", "Batch", "appsettings.Local.json");
    builder.Configuration.AddJsonFile(localSettingsPath, optional: true);
    builder.Configuration.AddEnvironmentVariables(prefix: "COMICAL_");
    builder.Services.AddBatchInfrastructure(builder.Configuration);

    using var host = builder.Build();
    await host.StartAsync();
    try
    {
        var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        await using (var scope = host.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SqlWarmup>().RunAsync(ct);
        }
        await host.Services.GetRequiredService<DailyFetchJob>().RunAsync(ct);
    }
    finally
    {
        await host.StopAsync();
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
