using Comical.Api.Models;
using Comical.Api.Repositories;
using ComiCal.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Comical.Api.Services
{
    public class ComicService : IComicService
    {
        private readonly IComicRepository _comicRepository;

        public ComicService(
            IComicRepository comicRepository
        )
        {
            _comicRepository = comicRepository;
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync(GetComicsRequest req, DateTime fromDate)
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
            
            // Pass keywords directly to repository for Cosmos DB query
            return await _comicRepository.GetComicsAsync(fromDate, keywords);
        }
    }
}
