using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComiCal.Shared.Models;
using Dapper;
using Npgsql;

namespace Comical.Api.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private const int MaxItemCount = 100;

        public ComicRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync(DateTime fromDate, IReadOnlyList<string>? keywords = null)
        {
            try
            {
                var queryBuilder = new StringBuilder();
                // Note: isbn maps to both id and Isbn properties in Comic model
                queryBuilder.Append(@"
                    SELECT 
                        isbn as Isbn,
                        type,
                        title as Title,
                        title_kana as TitleKana,
                        series_name as SeriesName,
                        series_name_kana as SeriesNameKana,
                        author as Author,
                        author_kana as AuthorKana,
                        publisher_name as PublisherName,
                        sales_date as SalesDate,
                        schedule_status as ScheduleStatus
                    FROM comics 
                    WHERE type = @type AND sales_date >= @fromDate");
                
                // Add keyword search conditions (AND logic for multiple keywords)
                // Using ILIKE for case-insensitive pattern matching with wildcards
                if (keywords != null && keywords.Count > 0)
                {
                    for (int i = 0; i < keywords.Count; i++)
                    {
                        queryBuilder.Append($" AND (title ILIKE @keyword{i} OR author ILIKE @keyword{i})");
                    }
                }
                
                // Add ORDER BY and pagination
                queryBuilder.Append(" ORDER BY sales_date DESC");
                queryBuilder.Append(" LIMIT @limit");

                // Build dynamic parameters
                var parameters = new DynamicParameters();
                parameters.Add("type", "comic");
                parameters.Add("fromDate", fromDate);
                parameters.Add("limit", MaxItemCount);
                
                if (keywords != null && keywords.Count > 0)
                {
                    for (int i = 0; i < keywords.Count; i++)
                    {
                        // Add wildcards for partial matching
                        parameters.Add($"keyword{i}", $"%{keywords[i]}%");
                    }
                }

                // Execute query using Dapper
                await using var connection = await _dataSource.OpenConnectionAsync();
                var results = await connection.QueryAsync<Comic>(queryBuilder.ToString(), parameters);
                
                // Set id property for Cosmos DB compatibility
                foreach (var comic in results)
                {
                    comic.id = comic.Isbn;
                }
                
                return results;
            }
            catch (NpgsqlException ex)
            {
                // Log and rethrow with context
                throw new InvalidOperationException($"Failed to retrieve comics from database: {ex.Message}", ex);
            }
        }
    }
}
