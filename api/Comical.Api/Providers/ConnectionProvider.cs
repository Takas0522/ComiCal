using Microsoft.Extensions.Configuration;
using System;

namespace Comical.Api.Providers
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
        /// <exception cref="InvalidOperationException">Thrown when connection string is not configured</exception>
        public string GetPostgresConnectionString()
        {
            var connectionString = _configuration.GetConnectionString(PostgresConnection);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{PostgresConnection}' is not configured.");
            }
            return connectionString;
        }
    }
}
