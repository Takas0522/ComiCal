using Castle.Core.Logging;
using ComiCal.Batch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ComiCal.Batch.Repositories
{
    public class RakutenComicRepository : IRakutenComicRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _applicationId;
        private readonly ILogger<RakutenComicRepository> _logger;

        public RakutenComicRepository(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<RakutenComicRepository> logger
        )
        {
            _httpClient = httpClient;
            _applicationId = configuration["applicationid"];
            _logger = logger;
        }

        public async Task<RakutenComicResponse> Fetch(int requestPage)
        {
            // Rakuten API rate limit: 1 request per second per Application ID
            // Wait 1 second before making the API call to comply with rate limits
            await Task.Delay(TimeSpan.FromSeconds(1));
            
            var sort = HttpUtility.UrlEncode("+releaseDate");
            var baseUrl = $"https://app.rakuten.co.jp/services/api/BooksBook/Search/20170404?booksGenreId=001001&sort={sort}&page={requestPage}&availability=5&applicationId={_applicationId}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            
            // Set timeout for the request
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
            var res = await _httpClient.SendAsync(requestMessage, cts.Token);
            
            if (res.StatusCode != HttpStatusCode.OK)
            {
                var errorMessage = await res.Content.ReadAsStringAsync();
                throw new Exception($"RakutenWebAPI Error\n{errorMessage}");
            }
            var content = await res.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<RakutenComicResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<BinaryData> FetchImageAndConvertStream(string imageUrl)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            var res = await _httpClient.SendAsync(requestMessage);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"ErrorCode:{res.StatusCode}/URL:{imageUrl}");
                return null;
            }
            Stream data = await res.Content.ReadAsStreamAsync();
            using (MemoryStream ms = new MemoryStream())
            {
                data.CopyTo(ms);
                return new BinaryData(ms.ToArray());
            }
        }
    }
}
