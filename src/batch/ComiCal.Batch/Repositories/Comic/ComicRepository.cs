using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Shared.Models;
using Npgsql;
using Dapper;

namespace ComiCal.Batch.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public ComicRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync()
        {
            const string sql = @"
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
                FROM comics";

            await using var connection = await _dataSource.OpenConnectionAsync();
            var results = await connection.QueryAsync<Comic>(sql);
            return results;
        }

        public async Task UpsertComicsAsync(IEnumerable<Comic> comics)
        {
            if (comics == null || !comics.Any())
            {
                return;
            }

            const string sql = @"
                INSERT INTO comics (
                    isbn, 
                    type, 
                    title, 
                    title_kana, 
                    series_name, 
                    series_name_kana, 
                    author, 
                    author_kana, 
                    publisher_name, 
                    sales_date, 
                    schedule_status
                ) VALUES (
                    @Isbn, 
                    @Type, 
                    @Title, 
                    @TitleKana, 
                    @SeriesName, 
                    @SeriesNameKana, 
                    @Author, 
                    @AuthorKana, 
                    @PublisherName, 
                    @SalesDate, 
                    @ScheduleStatus
                )
                ON CONFLICT (isbn) DO UPDATE SET
                    type = EXCLUDED.type,
                    title = EXCLUDED.title,
                    title_kana = EXCLUDED.title_kana,
                    series_name = EXCLUDED.series_name,
                    series_name_kana = EXCLUDED.series_name_kana,
                    author = EXCLUDED.author,
                    author_kana = EXCLUDED.author_kana,
                    publisher_name = EXCLUDED.publisher_name,
                    sales_date = EXCLUDED.sales_date,
                    schedule_status = EXCLUDED.schedule_status";

            // Prepare comics with default values
            var comicsToUpsert = comics.Select(c => new
            {
                c.Isbn,
                Type = string.IsNullOrWhiteSpace(c.type) ? "comic" : c.type,
                c.Title,
                c.TitleKana,
                c.SeriesName,
                c.SeriesNameKana,
                c.Author,
                c.AuthorKana,
                c.PublisherName,
                c.SalesDate,
                c.ScheduleStatus
            }).ToList();

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                // Execute batch upsert in a single transaction
                await connection.ExecuteAsync(sql, comicsToUpsert, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
