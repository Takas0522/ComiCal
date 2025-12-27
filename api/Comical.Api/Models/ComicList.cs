using ComiCal.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace Comical.Api.Models
{
    public class ComicList
    {
        private readonly IEnumerable<Comic> _comics;

        public ComicList(IEnumerable<Comic> comics)
        {
            _comics = comics;
        }

        public IEnumerable<Comic> GetComics() => _comics;

        public ComicList Search(IEnumerable<string> searchList)
        {
            var comics = searchList
                .SelectMany(keyword => _comics
                    .Where(w => w.Title.Contains(keyword)
                                || w.Author.Contains(keyword)))
                .Distinct()
                .OrderBy(o => o.SalesDate)
                .ToList();

            return new ComicList(comics);
        }

        public IEnumerable<string> GetIsbns()
        {
            return _comics.Select(c => c.Isbn).ToList();
        }
    }
}