using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComiCal.Shared.Providers
{
    public static class ConnectionName
    {
        public static string DefaultConnection = "DefaultConnection";
        public static string BlobConnection = "StorageConnectionString";
        public static string PostgresConnection = "DefaultConnection";
        public static string CosmosConnection = "CosmosConnectionString"; // Deprecated - use PostgresConnection
    } 

    public delegate DbConnection DefaultConnectionFactory();
    public delegate BlobServiceClient BlobClientFactory();
    public delegate CosmosClient CosmosClientFactory(); // Deprecated - to be removed after PostgreSQL migration

}
