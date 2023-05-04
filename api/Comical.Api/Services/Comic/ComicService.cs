using Comical.Api.Models;
using Comical.Api.Repositories;
using ComiCal.Shared.Models;
using System.Collections.Generic;
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

            var comics = new ComicList(data);

            var searchComics = comics.Search(req.SearchList);

            var isbns = searchComics.GetIsbns();

            var images = await _comicRepository.GetComicImagessAsync(isbns);

            var comicsWithImage = searchComics.GetComicsWithImage(images);

            return comicsWithImage.GetComics();
        }
    }
}
