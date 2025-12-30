using ComiCal.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comical.Api.Repositories
{
    public interface IComicRepository
    {
        Task<IEnumerable<Comic>> GetComicsAsync(DateTime fromDate, IReadOnlyList<string>? keywords = null);
    }
}