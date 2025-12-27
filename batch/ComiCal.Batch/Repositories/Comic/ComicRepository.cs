using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Shared.Models;
using ComiCal.Shared.Providers;

namespace ComiCal.Batch.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private const string DatabaseName = "ComiCalDB";
        private const string ContainerName = "comics";
        private const int MaxItemCount = 100;
        private const int MaxDegreeOfParallelism = 10;

        public ComicRepository(CosmosClientFactory cosmosClientFactory)
        {
            _cosmosClient = cosmosClientFactory();
            _container = _cosmosClient.GetContainer(DatabaseName, ContainerName);
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync()
        {
            var results = new List<Comic>();
            
            // Query all comics with type = "comic"
            var queryDefinition = new QueryDefinition(
                "SELECT * FROM c WHERE c.type = @type")
                .WithParameter("@type", "comic");

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

        public async Task UpsertComicsAsync(IEnumerable<Comic> comics)
        {
            if (comics == null || !comics.Any())
            {
                return;
            }

            // Use Bulk execution mode for better performance
            var tasks = new List<Task>();
            
            foreach (var comic in comics)
            {
                // Ensure required fields are set
                if (string.IsNullOrWhiteSpace(comic.id))
                {
                    comic.id = comic.Isbn; // Use ISBN as ID
                }
                if (string.IsNullOrWhiteSpace(comic.type))
                {
                    comic.type = "comic";
                }

                // Upsert each comic using bulk operations
                tasks.Add(_container.UpsertItemAsync(
                    comic,
                    new PartitionKey(comic.type),
                    new ItemRequestOptions { }
                ));
            }

            // Execute all upserts in parallel
            await Task.WhenAll(tasks);
        }
    }
}
