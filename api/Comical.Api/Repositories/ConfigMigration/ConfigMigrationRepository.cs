using Comical.Api.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using static Dapper.SqlMapper;

namespace Comical.Api.Repositories
{
    public class ConfigMigrationRepository : IConfigMigrationRepository
    {
        private readonly string _ConnectionString;
        public ConfigMigrationRepository(IConfiguration config)
        {
            _ConnectionString = config.GetConnectionString("DefaultConnection");
        }


        public async Task RegisterConfig(string id, string settings)
        {
            var param = new DynamicParameters();
            param.Add("@id", id);
            param.Add("@value", settings);

            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync("RegisterConfigMigrationData", param, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<ConfigMigration> GetConfigSettings(string id)
        {
            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                var res = await connection.QueryAsync<ConfigMigration>("GetConfigMigration", commandType: CommandType.StoredProcedure);
                if (res.Any())
                {
                    return res.First();
                }
                return null;
            }
        }

        public async Task DeleteConfigSettings(string id)
        {
            var param = new DynamicParameters();
            param.Add("@id", id);

            using (var connection = new SqlConnection(_ConnectionString))
            {
                connection.Open();
                await connection.ExecuteAsync("DeleteConfigMigration", param, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
