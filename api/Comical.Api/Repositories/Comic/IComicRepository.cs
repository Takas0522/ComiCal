using Comical.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comical.Api.Repositories
{
    public interface IComicRepository
    {
        Task<IEnumerable<Comic>> GetComicsAsync();
    }
}