using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComiCal.Shared.Models;
using ComiCal.Shared.Providers;
using Microsoft.Azure.Cosmos;

namespace Comical.Api.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private const string DatabaseName = "ComiCalDB";
        private const string ContainerName = "comics";

        public ComicRepository(CosmosClientFactory cosmosClientFactory)
        {
            _cosmosClient = cosmosClientFactory();
            _container = _cosmosClient.GetContainer(DatabaseName, ContainerName);
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync(DateTime fromDate)
        {
            var results = new List<Comic>();
            
            // Build query: WHERE c.type = "comic" AND c.salesDate >= @fromDate
            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type AND c.salesDate >= @fromDate")
                .WithParameter("@type", "comic")
                .WithParameter("@fromDate", fromDate);

            string? continuationToken = null;

            do
            {
                var queryRequestOptions = new QueryRequestOptions
                {
                    MaxItemCount = 100
                };

                using FeedIterator<Comic> feedIterator = _container.GetItemQueryIterator<Comic>(
                    queryDefinition,
                    continuationToken,
                    queryRequestOptions);

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Comic> response = await feedIterator.ReadNextAsync();
                    results.AddRange(response);
                    continuationToken = response.ContinuationToken;
                }
            } while (continuationToken != null);

            return results;
        }
    }
}
