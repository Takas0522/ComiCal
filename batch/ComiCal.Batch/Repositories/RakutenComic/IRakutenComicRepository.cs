using ComiCal.Batch.Models;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public interface IRakutenComicRepository
    {
        Task<RakutenComicResponse> Fetch(int requestPage);
        Task<string> FetchImageAndConvertBase64(string imageUrl);
    }
}