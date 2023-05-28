using Azure.Storage.Blobs;
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
    } 

    public delegate DbConnection DefaultConnectionFactory();
    public delegate BlobServiceClient BlobClientFactory();

}
