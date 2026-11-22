using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Hosting;

public interface IFailureNotifier
{
    Task SendAsync(int exitCode);
}

public interface IHeartbeat
{
    Task SendAsync(int exitCode);
}

public sealed partial class FailureNotifier(
    IHttpClientFactory clients, IConfiguration configuration, TimeProvider clock,
    ILogger<FailureNotifier> logger) : IFailureNotifier
{
    public async Task SendAsync(int exitCode)
    {
        var url = configuration["Batch:FailureWebhookUrl"];
        if (!ExitCodes.IsFailure(exitCode) || string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            using var client = clients.CreateClient("BatchOperations");
            using var response = await client.PostAsJsonAsync(url,
                new
                {
                    // Slack / Teams の Incoming Webhook はメッセージ本文として text を要求する（docs/init.md §9.2）。
                    text = $"[ComiCal Batch] 失敗（終了コード {exitCode}）: {ExitCodes.Summary(exitCode)}",
                    timestamp = clock.GetUtcNow(),
                    exitCode,
                    summary = ExitCodes.Summary(exitCode)
                });
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            LogFailure(logger, ex.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failure webhook could not be delivered ({ErrorType}).")]
    private static partial void LogFailure(ILogger logger, string errorType);
}

public sealed partial class Heartbeat(
    IHttpClientFactory clients, IConfiguration configuration, ILogger<Heartbeat> logger) : IHeartbeat
{
    public async Task SendAsync(int exitCode)
    {
        var url = configuration["Batch:HeartbeatUrl"];
        if ((exitCode != ExitCodes.Success && !ExitCodes.IsFailure(exitCode)) || string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            var uri = new UriBuilder(url);
            if (exitCode != ExitCodes.Success)
                uri.Path = uri.Path.TrimEnd('/') + "/fail";

            using var client = clients.CreateClient("BatchOperations");
            using var response = await client.GetAsync(uri.Uri);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            LogFailure(logger, ex.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Heartbeat could not be delivered ({ErrorType}).")]
    private static partial void LogFailure(ILogger logger, string errorType);
}
