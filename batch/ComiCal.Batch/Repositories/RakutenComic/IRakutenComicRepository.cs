using ComiCal.Batch.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public interface IRakutenComicRepository
    {
        Task<RakutenComicResponse> Fetch(int requestPage);
        Task<BinaryData> FetchImageAndConvertStream(string imageUrl);
    }
}