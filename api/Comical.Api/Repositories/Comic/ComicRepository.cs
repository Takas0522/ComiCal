using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Comical.Api.Models;
using System.Data;
using Comical.Api.Util.Extensions;
using System.Linq;

namespace Comical.Api.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly string _ConnectionString;
        public ComicRepository(IConfiguration config)
        {
            _ConnectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<ComicImage>> GetComicImagessAsync(IEnumerable<string> isbns)
        {
            var param = new DynamicParameters();
            var d = isbns.Select(s => new { Isbn = s });
            var dt = d.ToDataTable();
            param.Add("@isbns", dt.AsTableValuedParameter("[dbo].[IsbnListTableType]"));

            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<ComicImage>("GetComicImages", param, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IEnumerable<Comic>> GetComicsAsync()
        {
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<Comic>("GetComics", commandType: CommandType.StoredProcedure);
            }
        }
    }
}
