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
        private const int MaxItemCount = 100;

        public ComicRepository(CosmosClientFactory cosmosClientFactory)
        {
            _cosmosClient = cosmosClientFactory();
            _container = _cosmosClient.GetContainer(DatabaseName, ContainerName);
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync(DateTime fromDate, IReadOnlyList<string>? keywords = null)
        {
            var results = new List<Comic>();
            
            // Build query text
            var queryText = "SELECT * FROM c WHERE c.type = @type AND c.SalesDate >= @fromDate";
            
            // Add keyword search conditions (AND logic for multiple keywords)
            if (keywords != null && keywords.Any())
            {
                for (int i = 0; i < keywords.Count; i++)
                {
                    var paramName = $"@keyword{i}";
                    queryText += $" AND (CONTAINS(c.Title, {paramName}) OR CONTAINS(c.Author, {paramName}))";
                }
            }
            
            // Add ORDER BY
            queryText += " ORDER BY c.SalesDate DESC";
            
            // Build query definition with all parameters
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@type", "comic")
                .WithParameter("@fromDate", fromDate);
            
            if (keywords != null && keywords.Any())
            {
                for (int i = 0; i < keywords.Count; i++)
                {
                    queryDefinition.WithParameter($"@keyword{i}", keywords[i]);
                }
            }

            var queryRequestOptions = new QueryRequestOptions
            {
                MaxItemCount = MaxItemCount
            };

            using FeedIterator<Comic> feedIterator = _container.GetItemQueryIterator<Comic>(
                queryDefinition,
                continuationToken: null,
                queryRequestOptions);

            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Comic> response = await feedIterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}
