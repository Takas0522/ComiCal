using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ComiCal.Shared.Models;
using Npgsql;
using Dapper;
using Microsoft.Extensions.Logging;
using System;

namespace ComiCal.Batch.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<ComicRepository> _logger;

        public ComicRepository(NpgsqlDataSource dataSource, ILogger<ComicRepository> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync()
        {
            const string sql = @"
                SELECT 
                    isbn as Isbn,
                    title as Title,
                    titlekana as TitleKana,
                    seriesname as SeriesName,
                    seriesnamekana as SeriesNameKana,
                    author as Author,
                    authorkana as AuthorKana,
                    publishername as PublisherName,
                    salesdate as SalesDate,
                    schedulestatus as ScheduleStatus
                FROM comic";

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
                INSERT INTO comic (
                    isbn, 
                    title, 
                    titlekana, 
                    seriesname, 
                    seriesnamekana, 
                    author, 
                    authorkana, 
                    publishername, 
                    salesdate, 
                    schedulestatus
                ) VALUES (
                    @Isbn, 
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
                    title = EXCLUDED.title,
                    titlekana = EXCLUDED.titlekana,
                    seriesname = EXCLUDED.seriesname,
                    seriesnamekana = EXCLUDED.seriesnamekana,
                    author = EXCLUDED.author,
                    authorkana = EXCLUDED.authorkana,
                    publishername = EXCLUDED.publishername,
                    salesdate = EXCLUDED.salesdate,
                    schedulestatus = EXCLUDED.schedulestatus";

            // Prepare comics with default values
            var comicsToUpsert = comics.Select(c => new
            {
                c.Isbn,
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
                
                _logger.LogDebug("Successfully upserted {Count} comics", comicsToUpsert.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert comics. Rolling back transaction. Count: {Count}", comicsToUpsert.Count);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
