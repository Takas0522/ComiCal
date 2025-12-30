using ComiCal.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComiCal.Batch.Repositories
{
    public interface IComicRepository
    {
        Task<IEnumerable<Comic>> GetComicsAsync();
        Task UpsertComicsAsync(IEnumerable<Comic> comics);
    }
}