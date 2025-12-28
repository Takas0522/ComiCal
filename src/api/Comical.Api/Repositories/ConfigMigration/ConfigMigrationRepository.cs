using Comical.Api.Models;
using Dapper;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Comical.Api.Repositories
{
    public class ConfigMigrationRepository : IConfigMigrationRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        private const string UpsertSql = "INSERT INTO config_migrations (id, value) VALUES (@Id, @Value) ON CONFLICT (id) DO UPDATE SET value = EXCLUDED.value";
        private const string SelectSql = "SELECT id, value FROM config_migrations WHERE id = @Id";
        private const string DeleteSql = "DELETE FROM config_migrations WHERE id = @Id";

        public ConfigMigrationRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public async Task RegisterConfig(string id, string settings)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(UpsertSql, new { Id = id, Value = settings });
        }

        public async Task<ConfigMigration?> GetConfigSettings(string id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<ConfigMigration>(SelectSql, new { Id = id });
        }

        public async Task DeleteConfigSettings(string id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(DeleteSql, new { Id = id });
        }
    }
}
