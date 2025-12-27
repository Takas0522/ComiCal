using ComiCal.Batch.Models;
using ComiCal.Batch.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Batch.Util.Common;
using System.IO;
using Castle.Core.Logging;
using ComiCal.Shared.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComiCal.Shared.Util;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Services
{
    public class ComicService : IComicService
    {
        private readonly IRakutenComicRepository _rakutenComicRepository;
        private readonly IComicRepository _comicRepository;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ComicService> _logger;
        private const string ContainerName = "images";

        public ComicService(
            IRakutenComicRepository rakutenComicRepository,
            IComicRepository comicRepository,
            BlobServiceClient blobServiceClient,
            IHttpClientFactory httpClientFactory,
            ILogger<ComicService> logger
        )
        {
            _rakutenComicRepository = rakutenComicRepository;
            _comicRepository = comicRepository;
            _blobServiceClient = blobServiceClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<int> GetPageCountAsync()
        {
            RakutenComicResponse data = await _rakutenComicRepository.Fetch(1);
            return data.PageCount;
        }

        public async Task RegitoryAsync(int requestPage)
        {
            RakutenComicResponse baseData = await _rakutenComicRepository.Fetch(requestPage);
            IEnumerable<Comic> comics = GenRegistData(baseData);

            await _comicRepository.UpsertComicsAsync(comics);
        }

        private IEnumerable<Comic> GenRegistData(RakutenComicResponse data)
        {
            return data.Comics.Select(x =>
            {
                var date = DateTimeUtility.JpDateToDateTimeType(x.Info.SalesDate);
                return new Comic
                {
                    Author = x.Info.Author,
                    AuthorKana = x.Info.AuthorKana,
                    Isbn = x.Info.Isbn,
                    PublisherName = x.Info.PublisherName,
                    SalesDate = date.value,
                    SeriesName = x.Info.SeriesName,
                    SeriesNameKana = x.Info.SeriesNameKana,
                    Title = x.Info.Title,
                    TitleKana = x.Info.TitleKana,
                    ScheduleStatus = (int)date.status
                };
            });
        }

        public async Task<IEnumerable<Comic>> GetUpdateImageTargetAsync()
        {
            // Get all comics from Cosmos DB
            var comics = await _comicRepository.GetComicsAsync();
            
            // Get blob container
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            
            var comicsNeedingImages = new List<Comic>();
            
            foreach (var comic in comics)
            {
                if (string.IsNullOrWhiteSpace(comic.Isbn))
                {
                    continue;
                }
                
                // Check if any image exists for this ISBN in blob storage using prefix search
                // This is more efficient than checking each extension individually
                var hasImage = false;
                await foreach (var blob in containerClient.GetBlobsAsync(prefix: comic.Isbn))
                {
                    // Check if the blob name matches the expected pattern: {isbn}.{ext}
                    if (blob.Name.StartsWith(comic.Isbn + "."))
                    {
                        hasImage = true;
                        break;
                    }
                }
                
                if (!hasImage)
                {
                    comicsNeedingImages.Add(comic);
                }
            }
            
            return comicsNeedingImages;
        }

        public async Task UpdateImageDataAsync(string isbn, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                throw new ArgumentException("ISBN cannot be null or whitespace.", nameof(isbn));
            }
            
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new ArgumentException("Image URL cannot be null or whitespace.", nameof(imageUrl));
            }
            
            try
            {
                // Download image from URL
                using var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                
                // Get content type and determine extension
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var extension = ContentTypeHelper.GetExtensionFromContentType(contentType);
                
                // Get blob container
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                
                // Ensure container exists with public access
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                // Create blob client with path: {isbn}.{ext}
                var blobName = $"{isbn}{extension}";
                var blobClient = containerClient.GetBlobClient(blobName);
                
                // Upload image to blob storage
                using var imageStream = await response.Content.ReadAsStreamAsync();
                await blobClient.UploadAsync(
                    imageStream,
                    new BlobHttpHeaders { ContentType = contentType },
                    conditions: null
                );
            }
            catch (HttpRequestException ex)
            {
                // Log error but don't throw - allow batch to continue with other images
                _logger.LogError(ex, "Failed to download image for ISBN {Isbn} from {ImageUrl}", isbn, imageUrl);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow batch to continue with other images
                _logger.LogError(ex, "Failed to upload image for ISBN {Isbn}", isbn);
            }
        }
    }
}
