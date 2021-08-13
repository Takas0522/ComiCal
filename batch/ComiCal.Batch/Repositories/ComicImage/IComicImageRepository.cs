using System;
using System.IO;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public interface IComicImageRepository
    {
        Task UploadImageAsync(string fileName, BinaryData content);
        Task DeleteImageAsync(string fileName);
    }
}