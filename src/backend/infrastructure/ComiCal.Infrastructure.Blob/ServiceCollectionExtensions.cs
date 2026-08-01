using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure.Blob;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlobInfrastructure(
        this IServiceCollection services, string storageAccountUri)
    {
        services.AddSingleton(_ => CreateBlobServiceClient(storageAccountUri));
        services.AddScoped<BlobStorageService>();
        return services;
    }

    private static BlobServiceClient CreateBlobServiceClient(string storageAccountUri)
    {
        // Azurite / connection-string mode (ローカル開発)
        if (storageAccountUri.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase) ||
            storageAccountUri.StartsWith("DefaultEndpointsProtocol=", StringComparison.OrdinalIgnoreCase))
        {
            return new BlobServiceClient(storageAccountUri);
        }

        // HTTP/HTTPS URI (Azurite, local storage)
        if (storageAccountUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            storageAccountUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new BlobServiceClient(new Uri(storageAccountUri));
        }

        // Azure 本番: URI + DefaultAzureCredential (Managed Identity)
        return new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential());
    }
}
