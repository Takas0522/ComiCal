using Microsoft.Extensions.Configuration;
using System;

namespace ComiCal.Batch.Providers
{
    /// <summary>
    /// Provides connection string configuration for database connections
    /// </summary>
    public class ConnectionProvider
    {
        /// <summary>
        /// PostgreSQL connection string configuration key
        /// </summary>
        public const string PostgresConnection = "DefaultConnection";

        private readonly IConfiguration _configuration;

        public ConnectionProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets the PostgreSQL connection string from configuration
        /// </summary>
        /// <returns>PostgreSQL connection string</returns>
        public string GetPostgresConnectionString()
        {
            return _configuration.GetConnectionString(PostgresConnection);
        }
    }
}
