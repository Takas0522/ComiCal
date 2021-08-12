using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
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

        [FunctionName("ConfigMigrationGet")]
        public async Task<IActionResult> GetConfigData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ConfigMigration")] HttpRequest req,
            ILogger log)
        {
            string id = req.Query["id"];
            IEnumerable<string> resValue = await _configMigrationService.LoadMigrationSetting(id);
            var res = new ConfigMigrationGetResponse { Data = resValue };
            return new OkObjectResult(res);
        }

        [FunctionName("ConfigMigrationPost")]
        public async Task<IActionResult> RegisterConfigData(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ConfigMigration")] HttpRequest req,
            ILogger log
        )
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var regData = JsonSerializer.Deserialize<IEnumerable<string>>(requestBody);
            string id = await _configMigrationService.RegisterMigrationSetting(regData);
            var res = new ConfigMigrationPostResponse { Id = id };
            return new OkObjectResult(res);
        }
    }
}
