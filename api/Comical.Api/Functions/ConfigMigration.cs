using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Comical.Api.Services;
using System.Collections.Generic;
using Utf8Json;
using Comical.Api.Models;

namespace Comical.Api.Functions
{
    public class ConfigMigration
    {
        private readonly IConfigMigrationService _configMigrationService;
        public ConfigMigration(
            IConfigMigrationService configMigrationService
        )
        {
            _configMigrationService = configMigrationService;
        }

        [Function("ConfigMigrationGet")]
        public async Task<HttpResponseData> GetConfigData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ConfigMigration")] HttpRequestData req,
            ILogger log)
        {
            var query = req.Query;
            string id = query["id"] ?? string.Empty;
            
            IEnumerable<string> resValue = await _configMigrationService.LoadMigrationSetting(id);
            var res = new ConfigMigrationGetResponse { Data = resValue };
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.ToJsonString(res));
            
            return response;
        }

        [Function("ConfigMigrationPost")]
        public async Task<HttpResponseData> RegisterConfigData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ConfigMigration")] HttpRequestData req,
            ILogger log
        )
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var regData = JsonSerializer.Deserialize<IEnumerable<string>>(requestBody);
            string id = await _configMigrationService.RegisterMigrationSetting(regData);
            var res = new ConfigMigrationPostResponse { Id = id };
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.ToJsonString(res));
            
            return response;
        }
    }
}
