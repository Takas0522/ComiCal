using Dapper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using ComiCal.Shared.Models;
using ComiCal.Shared.Util.Extensions;

namespace ComiCal.Batch.Repositories
{
    public class ComicRepository : IComicRepository
    {
        private readonly string _ConnectionString;
        public ComicRepository(IConfiguration config)
        {
            _ConnectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task RegisterComicsAsync(IEnumerable<Comic> datas, IEnumerable<ComicImage> comicImages)
        {
            var param = new DynamicParameters();
            var dt = datas.ToDataTable();
            var dtImage = comicImages.ToDataTable();
            param.Add("@comics", dt.AsTableValuedParameter("[dbo].[ComicTableType]"));
            param.Add("@comicsImage", dtImage.AsTableValuedParameter("[dbo].[ComicImageTableType]"));
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync("RegisterComics", param, commandType: CommandType.StoredProcedure);
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

        public async Task<IEnumerable<ComicImage>> GetUpdateImageTargetAsync()
        {
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                return await connection.QueryAsync<ComicImage>("GetUpdateImageTarget", commandType: CommandType.StoredProcedure);
            }
        }

        public async Task RegisterComicImageUrlAsync(string isbn, string storgaeUrl)
        {
            var param = new DynamicParameters();
            param.Add("@isbn", isbn);
            param.Add("@imageStorageUrl", storgaeUrl);
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync("RegisterComicImage", param, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<ComicImage> GetComicImageInfo(string isbn)
        {
            var param = new DynamicParameters();
            param.Add("@isbn", isbn);
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                IEnumerable<ComicImage> data = await connection.QueryAsync<ComicImage>("GetComicImage", param, commandType: CommandType.StoredProcedure);
                if (data.Any())
                {
                    return data.First();
                }
                return null;
            }
        }
    }
}
