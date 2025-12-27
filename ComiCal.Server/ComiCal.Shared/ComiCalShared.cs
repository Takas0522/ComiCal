using ComiCal.Shared.Providers;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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
                return new SqlConnection(accessKey);
            }
            service.AddSingleton<DefaultConnectionFactory>(() => ConnectionFactory(config.GetConnectionString(ConnectionName.DefaultConnection)));

            // BlobClient
            var blobConnection = config["StorageConnectionString"];
            service.AddAzureClients(clientBuilder => {
                clientBuilder.AddBlobServiceClient(blobConnection);
            });

            // CosmosClient Factory
            service.AddSingleton<CosmosClientFactory>(() =>
            {
                var cosmosConnection = config[ConnectionName.CosmosConnection];
                return new CosmosClient(cosmosConnection);
            });
        }
    }
}
