using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public class ComicImageRepository : IComicImageRepository
    {
        private readonly string _connectionString;
        private readonly BlobServiceClient _client;
        private readonly string _containerName = "image";
        private readonly BlobContainerClient _containerClient;

        public ComicImageRepository(
            IConfiguration config
        )
        {
            _connectionString = config["StorageConnectionString"];
            _client = new BlobServiceClient(_connectionString);
            _containerClient = _client.GetBlobContainerClient(_containerName);
        }

        public async Task DeleteImageAsync(string fileName)
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task UploadImageAsync(string fileName, BinaryData content)
        {

            BlobClient blobClient = _containerClient.GetBlobClient(fileName);
            bool existsBlobData = await blobClient.ExistsAsync();
            if (existsBlobData)
            {
                return;
            }
            await blobClient.UploadAsync(content);

            BlobHttpHeaders headers = new BlobHttpHeaders();

            var s = new FileExtensionContentTypeProvider();
            s.TryGetContentType(fileName, out var contentType);

            headers.CacheControl = "public, max-age=15552000";
            headers.ContentType = contentType;

            await blobClient.SetHttpHeadersAsync(headers);
        }
    }
}
