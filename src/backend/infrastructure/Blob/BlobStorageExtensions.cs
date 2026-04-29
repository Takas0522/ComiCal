using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ComiCal.Infrastructure.Blob;

/// <summary><see cref="BlobServiceClient"/> + <see cref="ICoverBlobStorage"/> を登録する DI 拡張。</summary>
public static class BlobStorageExtensions
{
    public static IServiceCollection AddCoverBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.AccountUri))
            {
                return new BlobServiceClient(new Uri(opts.AccountUri), new DefaultAzureCredential());
            }
            if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
            {
                return new BlobServiceClient(opts.ConnectionString);
            }
            throw new InvalidOperationException(
                "Storage:AccountUri or Storage:ConnectionString must be configured.");
        });

        services.AddSingleton<ICoverBlobStorage, BlobCoverStorage>();
        return services;
    }
}
