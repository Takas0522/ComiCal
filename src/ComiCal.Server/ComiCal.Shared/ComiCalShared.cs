using ComiCal.Shared.Providers;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using Npgsql;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Azure.Identity;

namespace ComiCal.Shared
{
    public static class ComiCalShared
    {
        public static void AddComicalStartupSharedConfiguration(this IServiceCollection service, IConfiguration config)
        {

            // Database Factory
            static DbConnection ConnectionFactory(string accessKey)
            {
                return new NpgsqlConnection(accessKey);
            }
            service.AddSingleton<DefaultConnectionFactory>(() => ConnectionFactory(config.GetConnectionString(ConnectionName.DefaultConnection)));

            // NpgsqlDataSource as Singleton (recommended pattern for Npgsql 8.0+)
            service.AddSingleton<NpgsqlDataSource>(sp =>
            {
                var connectionString = config.GetConnectionString(ConnectionName.PostgresConnection);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException($"PostgreSQL connection string '{ConnectionName.PostgresConnection}' is not configured.");
                }
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                return dataSourceBuilder.Build();
            });

            // BlobClient - Managed Identity support with fallback to connection string
            var storageAccountName = config["StorageAccountName"];
            var storageConnectionString = config["StorageConnectionString"];
            
            service.AddAzureClients(clientBuilder => {
                // Prioritize Managed Identity when StorageAccountName is configured
                if (!string.IsNullOrWhiteSpace(storageAccountName))
                {
                    // Use Managed Identity via DefaultAzureCredential
                    var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
                    
                    clientBuilder.AddBlobServiceClient(blobServiceUri)
                        .WithCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                        {
                            // Configure retry settings for better reliability
                            Retry = 
                            {
                                MaxRetries = 3,
                                NetworkTimeout = TimeSpan.FromSeconds(30)
                            }
                        }));
                    
                    // StorageConnectionString is retained as fallback for Managed Identity failures
                    // and can be used in development/local environments
                }
                else if (!string.IsNullOrWhiteSpace(storageConnectionString))
                {
                    // Fall back to connection string authentication when StorageAccountName is not set
                    clientBuilder.AddBlobServiceClient(storageConnectionString);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Azure Storage configuration is missing. Please configure either 'StorageAccountName' " +
                        "(for Managed Identity) or 'StorageConnectionString' (for connection string authentication).");
                }
            });

            // CosmosClient as Singleton (Deprecated - to be removed after PostgreSQL migration)
            service.AddSingleton<CosmosClient>(sp =>
            {
                var cosmosConnection = config[ConnectionName.CosmosConnection];
                if (string.IsNullOrWhiteSpace(cosmosConnection))
                {
                    throw new InvalidOperationException($"Cosmos DB connection string '{ConnectionName.CosmosConnection}' is not configured.");
                }
                return new CosmosClient(cosmosConnection);
            });

            // CosmosClient Factory (Deprecated - to be removed after PostgreSQL migration)
            service.AddSingleton<CosmosClientFactory>(sp => () => sp.GetRequiredService<CosmosClient>());
        }
    }
}
