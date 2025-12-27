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
            // Pass keywords directly to repository for Cosmos DB query
            var keywords = req.SearchList?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
            return await _comicRepository.GetComicsAsync(fromDate, keywords);
        }
    }
}
