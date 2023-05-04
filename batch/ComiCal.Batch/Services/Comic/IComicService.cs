using ComiCal.Batch.Models;
using ComiCal.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComiCal.Batch.Services
{
    public interface IComicService
    {
        Task RegitoryAsync(int requestPage);
        Task<int> GetPageCountAsync();
        Task<IEnumerable<ComicImage>> GetUpdateImageTargetAsync();
        Task UpdateImageDataAsync(ComicImage data);
    }
}