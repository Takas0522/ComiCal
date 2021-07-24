using ComiCal.Batch.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using ComiCal.Batch.Util.Extensions;
using System.Data;

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

        public async Task RegisterComicImageAsync(string isbn, string base64Text)
        {
            var param = new DynamicParameters();
            param.Add("@isbn", isbn);
            param.Add("@imageBase64Value", base64Text);
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync("RegisterComicImage", param, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
