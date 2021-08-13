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
        private readonly string _baserUrl = "https://stmanrim.blob.core.windows.net/image";

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
            var resData = res.Distinct().OrderBy(o => o.SalesDate).ToList();
            var isbns = res.Select(s => s.Isbn);
            var images = await _comicRepository.GetComicImagessAsync(isbns);
            resData.ForEach(f => {
                var i = images.Where(w => w.Isbn == f.Isbn);
                if (i.Any())
                {
                    f.ImageStorageUrl = _baserUrl + i.First().ImageStorageUrl;
                }
            });
            return resData;
        }
    }
}
