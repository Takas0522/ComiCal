using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace ComiCal.Batch.Hosting;

public interface IBatchSecretSource
{
    Task<string?> GetAsync(Uri vaultUri, string name, CancellationToken ct);
}

public sealed class KeyVaultSecretSource(TokenCredential credential) : IBatchSecretSource
{
    public async Task<string?> GetAsync(Uri vaultUri, string name, CancellationToken ct)
    {
        var client = new SecretClient(vaultUri, credential);
        try
        {
            return (await client.GetSecretAsync(name, cancellationToken: ct)).Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}

public sealed class KeyVaultSettings(IConfiguration configuration, IBatchSecretSource secrets)
{
    private static readonly (string Name, string Key, bool Required)[] Mappings =
    [
        ("RakutenAppId", "RakutenApplicationId", true),
        ("RakutenAccessKey", "RakutenAccessKey", true),
        ("RakutenAffiliateId", "RakutenAffiliateId", true),
        ("AppInsightsConnectionString", "APPLICATIONINSIGHTS_CONNECTION_STRING", false)
    ];

    public async Task LoadAsync(CancellationToken ct)
    {
        var vault = configuration["Azure:KeyVaultUri"];
        var uri = string.IsNullOrWhiteSpace(vault) ? null : new Uri(vault);
        var resolved = new Dictionary<string, string?>();
        foreach (var (name, key, required) in Mappings)
        {
            if (!string.IsNullOrWhiteSpace(configuration[key]))
                continue;

            var value = uri is null ? null : await secrets.GetAsync(uri, name, ct);
            if (!string.IsNullOrWhiteSpace(value))
                resolved[key] = value;
            else if (required)
                throw new InvalidOperationException($"Required setting {key} is missing.");
        }

        foreach (var (key, value) in resolved)
            configuration[key] = value;
    }
}
