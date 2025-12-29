using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Comical.Api.Models;
using Comical.Api.Services;
using Comical.Api.Util.Common;

namespace Comical.Api.Functions
{
    public class ConfigMigration
    {
        private readonly IConfigMigrationService _configMigrationService;
        private readonly ILogger<ConfigMigration> _logger;

        public ConfigMigration(
            IConfigMigrationService configMigrationService,
            ILogger<ConfigMigration> logger)
        {
            _configMigrationService = configMigrationService;
            _logger = logger;
        }

        [Function("ConfigMigrationGet")]
        public async Task<HttpResponseData> GetConfigData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ConfigMigration")] HttpRequestData req)
        {
            return await FunctionExecutionHelper.ExecuteAsync(req, _logger, async () =>
            {
                // Parse query string to get 'id' parameter
                string? id = null;
                var query = req.Url.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var queryParams = query.TrimStart('?').Split('&')
                        .Select(p => p.Split('='))
                        .Where(p => p.Length == 2)
                        .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
                    
                    if (queryParams.TryGetValue("id", out var idValue))
                    {
                        id = idValue;
                    }
                }

                IEnumerable<string> resValue = await _configMigrationService.LoadMigrationSetting(id);
                var res = new ConfigMigrationGetResponse { Data = resValue };

                return await HttpResponseHelper.CreateOkResponseAsync(req, res);
            });
        }

        [Function("ConfigMigrationPost")]
        public async Task<HttpResponseData> RegisterConfigData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ConfigMigration")] HttpRequestData req)
        {
            return await FunctionExecutionHelper.ExecuteAsync(req, _logger, async () =>
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var regData = JsonSerializer.Deserialize<IEnumerable<string>>(requestBody);

                string id = await _configMigrationService.RegisterMigrationSetting(regData);
                var res = new ConfigMigrationPostResponse { Id = id };

                return await HttpResponseHelper.CreateOkResponseAsync(req, res);
            });
        }
    }
}
