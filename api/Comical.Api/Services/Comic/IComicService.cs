using Comical.Api.Models;
using ComiCal.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comical.Api.Services
{
    public interface IComicService
    {
        Task<IEnumerable<Comic>> GetComics(GetComicsRequest req);
    }
}