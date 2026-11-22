using System.Globalization;
using Azure.Core;
using Azure.Identity;
using ComiCal.Batch.Hosting;
using ComiCal.Domain.Repositories;
using ComiCal.Infrastructure.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var command = BatchCommand.Parse(args);
try
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        ContentRootPath = AppContext.BaseDirectory
    });
    builder.Configuration.AddJsonFile(BatchPaths.LocalSettings, optional: true);
    builder.Configuration.AddEnvironmentVariables(prefix: "COMICAL_");

    var configuredLogDirectory = builder.Configuration["Batch:LogDirectory"];
    var logDirectory = string.IsNullOrWhiteSpace(configuredLogDirectory)
        ? BatchPaths.Logs : Path.GetFullPath(configuredLogDirectory, BatchPaths.Root);
    using var logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        .WriteTo.File(Path.Combine(logDirectory, "batch-.log"), formatProvider: CultureInfo.InvariantCulture,
            rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, retainedFileTimeLimit: TimeSpan.FromDays(30),
            shared: true)
        .CreateLogger();

    builder.Logging.ClearProviders();
    builder.Services.AddSerilog(logger);
    builder.Services.AddSingleton<Serilog.ILogger>(logger);
    builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
    builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
    builder.Services.AddHttpClient("BatchOperations", client => client.Timeout = TimeSpan.FromSeconds(5))
        .RemoveAllLoggers()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    builder.Services.AddSqlInfrastructure(builder.Configuration["SqlConnectionString"] ?? string.Empty,
        SqlInfrastructureOptions.BatchDefaults);
    builder.Services.AddSingleton<ISingleInstanceGuard, SingleInstanceGuard>();
    builder.Services.AddSingleton<IBatchSecretSource, KeyVaultSecretSource>();
    builder.Services.AddSingleton<KeyVaultSettings>();
    builder.Services.AddSingleton<IPreflightChecks, PreflightChecks>();
    builder.Services.AddSingleton<IPreflight, Preflight>();
    builder.Services.AddSingleton<IFailureNotifier, FailureNotifier>();
    builder.Services.AddSingleton<IHeartbeat, Heartbeat>();
    builder.Services.AddScoped<SqlWarmup>();
    builder.Services.AddScoped<IBatchWork, BatchWork>();
    builder.Services.AddScoped<IStaleBatchRunCleaner, StaleBatchRunCleaner>();
    builder.Services.AddScoped(sp => new Lazy<IBatchRunRepository>(sp.GetRequiredService<IBatchRunRepository>));
    builder.Services.AddScoped(sp => new Lazy<IStaleBatchRunCleaner>(sp.GetRequiredService<IStaleBatchRunCleaner>));
    builder.Services.AddScoped<BatchRunner>();

    using var host = builder.Build();
    await host.StartAsync();
    try
    {
        var ct = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        await using var scope = host.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<BatchRunner>()
            .Execute(command, command is null ? Console.Error : Console.Out, ct);
    }
    finally
    {
        await host.StopAsync();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    if (command is null)
    {
        Console.Error.WriteLine(BatchCommand.Usage);
        return ExitCodes.PreflightFailure;
    }

    return ExitCodes.Failure;
}
