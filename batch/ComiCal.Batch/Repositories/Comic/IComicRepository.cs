using ComiCal.Batch.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public interface IComicRepository
    {
        Task<IEnumerable<Comic>> GetComicsAsync();
        Task RegisterComicsAsync(IEnumerable<Comic> datas, IEnumerable<ComicImage> comicImages);
        Task<IEnumerable<ComicImage>> GetUpdateImageTargetAsync();
        Task RegisterComicImageAsync(string isbn, string base64Text);
    }
}