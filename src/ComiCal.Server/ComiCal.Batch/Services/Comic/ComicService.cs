using ComiCal.Batch.Models;
using ComiCal.Batch.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Logging;
using ComiCal.Shared.Models;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComiCal.Shared.Util;
using System.Net.Http;
using Npgsql;

namespace ComiCal.Batch.Services
{
    public class ComicService : IComicService
    {
        private readonly IRakutenComicRepository _rakutenComicRepository;
        private readonly IComicRepository _comicRepository;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ComicService> _logger;
        private const string ContainerName = "$web";

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

        private async Task<BlobContainerClient> GetImagesContainerClientAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);

            try
            {
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // Azure Storage returns 409 when the container already exists.
                // During local runs (and in races), CreateIfNotExistsAsync may still surface the conflict.
                // Treat it as success and continue.
                _logger.LogDebug(ex, "Blob container already exists (ignored): {ContainerName}", ContainerName);
            }

            return containerClient;
        }

        public async Task<int> GetPageCountAsync()
        {
            try
            {
                RakutenComicResponse data = await _rakutenComicRepository.Fetch(1);
                return data.PageCount;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while getting page count from Rakuten API");
                throw new InvalidOperationException("Failed to get page count due to HTTP error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get page count from Rakuten API");
                throw new InvalidOperationException("Failed to get page count", ex);
            }
        }

        public async Task RegitoryAsync(int requestPage)
        {
            try
            {
                RakutenComicResponse baseData = await _rakutenComicRepository.Fetch(requestPage);
                IEnumerable<Comic> comics = GenRegistData(baseData);

                await _comicRepository.UpsertComicsAsync(comics);
                _logger.LogDebug("Successfully registered comics from page {Page}", requestPage);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching comics for page {Page}", requestPage);
                throw new InvalidOperationException($"Failed to register comics for page {requestPage} due to HTTP error.", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while upserting comics for page {Page}", requestPage);
                throw new InvalidOperationException($"Failed to register comics for page {requestPage} due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register comics for page {Page}", requestPage);
                throw new InvalidOperationException($"Failed to register comics for page {requestPage}.", ex);
            }
        }

        private IEnumerable<Comic> GenRegistData(RakutenComicResponse data)
        {
            return data.Comics.Select(x =>
            {
                // Parse Japanese date format (likely YYYY年MM月DD日 format)
                DateTime? salesDate = null;
                if (!string.IsNullOrEmpty(x.Info.SalesDate))
                {
                    try
                    {
                        // Try common Japanese date formats
                        if (DateTime.TryParse(x.Info.SalesDate, out DateTime parsed))
                        {
                            salesDate = parsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to parse sales date: {x.Info.SalesDate}. Error: {ex.Message}");
                    }
                }

                return new Comic
                {
                    Author = x.Info.Author,
                    AuthorKana = x.Info.AuthorKana,
                    Isbn = x.Info.Isbn,
                    PublisherName = x.Info.PublisherName,
                    SalesDate = salesDate ?? DateTime.MinValue,
                    SeriesName = x.Info.SeriesName,
                    SeriesNameKana = x.Info.SeriesNameKana,
                    Title = x.Info.Title,
                    TitleKana = x.Info.TitleKana,
                    ScheduleStatus = salesDate.HasValue ? 1 : 0  // 1 if date parsed successfully, 0 otherwise
                };
            });
        }

        public async Task<IEnumerable<Comic>> GetUpdateImageTargetAsync()
        {
            try
            {
                // Get all comics from PostgreSQL
                var comics = await _comicRepository.GetComicsAsync();

                // Get blob container (ensure it exists)
                var containerClient = await GetImagesContainerClientAsync();
                
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
                    await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"{comic.Isbn}."))
                    {
                        // If any blob with prefix "{isbn}." exists, the comic has an image
                        hasImage = true;
                        break;
                    }
                    
                    if (!hasImage)
                    {
                        comicsNeedingImages.Add(comic);
                    }
                }
                
                _logger.LogDebug("Found {Count} comics needing images out of {Total} total comics", 
                    comicsNeedingImages.Count, comics.Count());
                return comicsNeedingImages;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving comics for image update");
                throw new InvalidOperationException("Failed to retrieve comics for image update due to database error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get comics needing image updates");
                throw;
            }
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
                var containerClient = await GetImagesContainerClientAsync();
                
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
                // Convert HttpRequestException to avoid Durable Functions serialization issues
                _logger.LogError(ex, "Failed to download image for ISBN {Isbn} from {ImageUrl}", isbn, imageUrl);
                throw new InvalidOperationException($"Failed to download image for ISBN {isbn}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image for ISBN {Isbn}", isbn);
                throw new InvalidOperationException($"Failed to process image for ISBN {isbn}", ex);
            }
        }

        public async Task ProcessImageDownloadsAsync(int pageNumber)
        {
            try
            {
                // Fetch comic data from Rakuten API for this page
                RakutenComicResponse data = await _rakutenComicRepository.Fetch(pageNumber);
                
                _logger.LogDebug("Processing images for page {Page} with {Count} comics", pageNumber, data.Comics.Count());
                
                // Process each comic's image
                foreach (var comicInfo in data.Comics)
                {
                    var isbn = comicInfo.Info.Isbn;
                    var imageUrl = comicInfo.Info.LargeImageUrl;
                    
                    // Skip if no ISBN or no image URL
                    if (string.IsNullOrWhiteSpace(isbn) || string.IsNullOrWhiteSpace(imageUrl))
                    {
                        _logger.LogDebug("Skipping comic with ISBN {Isbn} - missing ISBN or image URL", isbn);
                        continue;
                    }
                    
                    // Check if image already exists
                    var containerClient = await GetImagesContainerClientAsync();
                    
                    var hasImage = false;
                    await foreach (var blob in containerClient.GetBlobsAsync(prefix: $"{isbn}."))
                    {
                        hasImage = true;
                        break;
                    }
                    
                    if (hasImage)
                    {
                        _logger.LogDebug("Image already exists for ISBN {Isbn}, skipping", isbn);
                        continue;
                    }
                    
                    // Download and save the image
                    await UpdateImageDataAsync(isbn, imageUrl);
                    _logger.LogDebug("Successfully processed image for ISBN {Isbn}", isbn);
                }
                
                _logger.LogDebug("Completed image processing for page {Page}", pageNumber);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while processing images for page {Page}", pageNumber);
                throw new InvalidOperationException($"Failed to process images for page {pageNumber} due to HTTP error", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process images for page {Page}", pageNumber);
                throw new InvalidOperationException($"Failed to process images for page {pageNumber}", ex);
            }
        }
    }
}
