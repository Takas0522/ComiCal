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
        if (!RequiresTokenCredential(storageAccountUri))
        {
            return storageAccountUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? new BlobServiceClient(new Uri(storageAccountUri))
                : new BlobServiceClient(storageAccountUri);
        }

        return new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential());
    }

    internal static bool RequiresTokenCredential(string storageAccountUri)
    {
        if (storageAccountUri.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase) ||
            storageAccountUri.StartsWith("DefaultEndpointsProtocol=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return new Uri(storageAccountUri).Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
