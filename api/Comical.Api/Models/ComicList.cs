using System.Collections.Generic;
using System.Linq;

namespace Comical.Api.Models
{
    public class ComicList
    {
        private const string BaserUrl = "https://stmanrim.blob.core.windows.net/image";

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

        public ComicList GetComicsWithImage(IEnumerable<ComicImage> comicImages)
        {
            foreach (var comic in _comics)
            {
                var image = comicImages.FirstOrDefault(c => c.Isbn == comic.Isbn);

                if (!string.IsNullOrEmpty(image?.ImageStorageUrl))
                {
                    comic.ImageStorageUrl = BaserUrl + image.ImageStorageUrl;
                }
            }

            return new ComicList(_comics);
        }
    }
}