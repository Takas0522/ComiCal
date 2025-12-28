using Comical.Api.Models;
using Comical.Api.Repositories;
using ComiCal.Shared.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Comical.Api.Services
{
    public class ComicService : IComicService
    {
        private readonly IComicRepository _comicRepository;
        private readonly ILogger<ComicService> _logger;

        public ComicService(
            IComicRepository comicRepository,
            ILogger<ComicService> logger
        )
        {
            _comicRepository = comicRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync(GetComicsRequest req, DateTime fromDate)
        {
            try
            {
                // Return empty list if no search keywords provided (preserving original behavior)
                if (req.SearchList == null || !req.SearchList.Any())
                {
                    return new List<Comic>();
                }
                
                // Filter out null/whitespace keywords
                var keywords = req.SearchList.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray();
                
                // Return empty list if all keywords were null/whitespace
                if (keywords.Length == 0)
                {
                    return new List<Comic>();
                }
                
                // Pass keywords directly to repository for PostgreSQL query
                return await _comicRepository.GetComicsAsync(fromDate, keywords);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while retrieving comics. FromDate: {FromDate}, Keywords: {Keywords}", 
                    fromDate, req.SearchList != null ? string.Join(", ", req.SearchList) : "none");
                throw new InvalidOperationException("Failed to retrieve comics due to database error. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while retrieving comics. FromDate: {FromDate}", fromDate);
                throw;
            }
        }
    }
}
