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

        public ConfigMigrationRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public async Task RegisterConfig(string id, string settings)
        {
            const string sql = @"
                INSERT INTO config_migrations (id, value) 
                VALUES (@Id, @Value) 
                ON CONFLICT (id) 
                DO UPDATE SET value = EXCLUDED.value";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = id, Value = settings });
        }

        public async Task<ConfigMigration?> GetConfigSettings(string id)
        {
            const string sql = @"SELECT id, value FROM config_migrations WHERE id = @Id";

            await using var connection = await _dataSource.OpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<ConfigMigration>(sql, new { Id = id });
        }

        public async Task DeleteConfigSettings(string id)
        {
            const string sql = @"DELETE FROM config_migrations WHERE id = @Id";

            await using var connection = await _dataSource.OpenConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = id });
        }
    }
}
