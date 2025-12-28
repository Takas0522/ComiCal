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

            // BlobClient
            var blobConnection = config["StorageConnectionString"];
            service.AddAzureClients(clientBuilder => {
                clientBuilder.AddBlobServiceClient(blobConnection);
            });

            // CosmosClient as Singleton
            service.AddSingleton<CosmosClient>(sp =>
            {
                var cosmosConnection = config[ConnectionName.CosmosConnection];
                if (string.IsNullOrWhiteSpace(cosmosConnection))
                {
                    throw new InvalidOperationException($"Cosmos DB connection string '{ConnectionName.CosmosConnection}' is not configured.");
                }
                return new CosmosClient(cosmosConnection);
            });

            // CosmosClient Factory (for backward compatibility)
            service.AddSingleton<CosmosClientFactory>(sp => () => sp.GetRequiredService<CosmosClient>());
        }
    }
}
