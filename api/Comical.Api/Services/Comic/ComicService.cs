using Comical.Api.Models;
using Comical.Api.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IEnumerable<Comic>> GetComics(GetComicsRequest req)
        {
            IEnumerable<Comic> data = await _comicRepository.GetComicsAsync();
            var res = new List<Comic>();
            foreach (string keyword in req.SearchList)
            {
                var d = data.Where(w =>
                {
                    return (
                        w.Title.Contains(keyword) ||
                        w.Author.Contains(keyword)
                    );
                });
                res.AddRange(d);
            }
            return res.Distinct().OrderBy(o => o.SalesDate);
        }
    }
}
