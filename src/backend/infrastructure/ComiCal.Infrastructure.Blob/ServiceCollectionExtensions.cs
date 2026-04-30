using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace ComiCal.Infrastructure.Blob;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlobInfrastructure(
        this IServiceCollection services, string storageAccountUri)
    {
        services.AddSingleton(_ =>
            new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential()));
        services.AddScoped<BlobStorageService>();
        return services;
    }
}
