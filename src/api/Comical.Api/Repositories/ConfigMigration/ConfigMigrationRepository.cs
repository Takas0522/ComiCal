using Comical.Api.Models;
using ComiCal.Shared.Providers;
using Microsoft.Azure.Cosmos;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Comical.Api.Repositories
{
    public class ConfigMigrationRepository : IConfigMigrationRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private const string DatabaseName = "ComiCalDB";
        private const string ContainerName = "config-migrations";

        public ConfigMigrationRepository(CosmosClientFactory cosmosClientFactory)
        {
            _cosmosClient = cosmosClientFactory();
            _container = _cosmosClient.GetContainer(DatabaseName, ContainerName);
        }

        public async Task RegisterConfig(string id, string settings)
        {
            var configMigration = new ConfigMigration
            {
                id = id,
                Value = settings
            };

            // Upsert operation - creates if not exists, updates if exists
            await _container.UpsertItemAsync(configMigration, new PartitionKey(id));
        }

        public async Task<ConfigMigration?> GetConfigSettings(string id)
        {
            try
            {
                // Point read - most efficient operation (1 RU)
                var response = await _container.ReadItemAsync<ConfigMigration>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task DeleteConfigSettings(string id)
        {
            try
            {
                await _container.DeleteItemAsync<ConfigMigration>(id, new PartitionKey(id));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Item doesn't exist, which is fine for delete operation
            }
        }
    }
}
