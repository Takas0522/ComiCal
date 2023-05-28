using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using ComiCal.Shared.Util.Extensions;
using System.Linq;
using ComiCal.Shared.Models;
using ComiCal.Shared.Providers;

namespace Comical.Api.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly DefaultConnectionFactory _factory;
        public ComicRepository(DefaultConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<ComicImage>> GetComicImagessAsync(IEnumerable<string> isbns)
        {
            var param = new DynamicParameters();
            var d = isbns.Select(s => new { Isbn = s });
            var dt = d.ToDataTable();
            param.Add("@isbns", dt.AsTableValuedParameter("[dbo].[IsbnListTableType]"));

            using (var connection = _factory())
            {
                connection.Open();
                return await connection.QueryAsync<ComicImage>("GetComicImages", param, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync()
        {
            using (var connection = _factory())
            {
                connection.Open();
                return await connection.QueryAsync<Comic>("GetComics", commandType: CommandType.StoredProcedure);
            }
        }
    }
}
