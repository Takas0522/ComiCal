using System.Net;
using Azure.Core;
using ComiCal.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Hosting;

public interface IPreflight
{
    Task<bool> RunAsync(CancellationToken ct);
}

public interface IPreflightChecks
{
    Task CheckTokenAsync(CancellationToken ct);
    Task CheckPublicIpAsync(CancellationToken ct);
    Task CheckSecretsAsync(CancellationToken ct);
    Task CheckSqlAsync(CancellationToken ct);
}

public sealed partial class Preflight(IPreflightChecks checks, ILogger<Preflight> logger) : IPreflight
{
    public async Task<bool> RunAsync(CancellationToken ct)
    {
        var tokenOk = await CheckAsync("Azure SQL token", checks.CheckTokenAsync, ct);
        var ipOk = await CheckAsync("Public IP", checks.CheckPublicIpAsync, ct);
        var secretsOk = await CheckAsync("Key Vault settings", checks.CheckSecretsAsync, ct);
        var sqlOk = await CheckAsync("SQL connection", checks.CheckSqlAsync, ct);
        return tokenOk && ipOk && secretsOk && sqlOk;
    }

    private async Task<bool> CheckAsync(string name, Func<CancellationToken, Task> check, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await check(ct);
            return true;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            LogCheckFailed(logger, name, ex);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Preflight check failed: {Check}")]
    private static partial void LogCheckFailed(ILogger logger, string check, Exception exception);
}

public sealed class PreflightChecks(
    TokenCredential credential,
    IHttpClientFactory httpClients,
    KeyVaultSettings settings,
    IServiceScopeFactory scopes,
    IConfiguration configuration) : IPreflightChecks
{
    public async Task CheckTokenAsync(CancellationToken ct)
        => await credential.GetTokenAsync(new TokenRequestContext(["https://database.windows.net/.default"]), ct);

    public async Task CheckPublicIpAsync(CancellationToken ct)
    {
        var expected = configuration["Batch:ExpectedPublicIp"];
        if (string.IsNullOrWhiteSpace(expected))
            return;

        using var client = httpClients.CreateClient("BatchOperations");
        var actual = await client.GetStringAsync(new Uri("https://api.ipify.org"), ct);
        if (!IPAddress.TryParse(expected.Trim(), out var expectedIp) ||
            !IPAddress.TryParse(actual.Trim(), out var actualIp) || !expectedIp.Equals(actualIp))
            throw new InvalidOperationException("The public IP does not match Batch:ExpectedPublicIp.");
    }

    public Task CheckSecretsAsync(CancellationToken ct) => settings.LoadAsync(ct);

    public async Task CheckSqlAsync(CancellationToken ct)
    {
        // Allow for serverless resume while bounding the retry window for an unreachable SQL server.
        using var connectionTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectionTimeout.CancelAfter(TimeSpan.FromMinutes(3));
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ComiCalDbContext>();
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            await db.Database.OpenConnectionAsync(token);
            await db.Database.CloseConnectionAsync();
        }, connectionTimeout.Token);
    }
}
