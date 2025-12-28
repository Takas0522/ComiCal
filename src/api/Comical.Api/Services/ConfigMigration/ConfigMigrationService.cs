using Comical.Api.Models;
using Comical.Api.Repositories;
using Comical.Api.Util.Common;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comical.Api.Services
{
    public class ConfigMigrationService : IConfigMigrationService
    {
        private readonly IConfigMigrationRepository _configMigrationRepository;
        private readonly ILogger<ConfigMigrationService> _logger;
        private readonly string _separator = "[nextval]";

        public ConfigMigrationService(
            IConfigMigrationRepository configMigrationRepository,
            ILogger<ConfigMigrationService> logger
        )
        {
            _configMigrationRepository = configMigrationRepository;
            _logger = logger;
        }

        public async Task<string> RegisterMigrationSetting(IEnumerable<string> value)
        {
            try
            {
                string id = SimplifiedId.Generate(8, 0);
                ConfigMigration existData = await _configMigrationRepository.GetConfigSettings(id);
                while (existData != null)
                {
                    id = SimplifiedId.Generate(8, 0);
                    existData = await _configMigrationRepository.GetConfigSettings(id);
                }
                string saveVal = string.Join(_separator, value);
                await _configMigrationRepository.RegisterConfig(id, saveVal);
                
                _logger.LogInformation("Successfully registered migration setting with ID: {Id}", id);
                return id;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while registering migration setting");
                throw new InvalidOperationException("Failed to register migration setting due to database error. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while registering migration setting");
                throw;
            }
        }

        public async Task<IEnumerable<string>> LoadMigrationSetting(string id)
        {
            try
            {
                ConfigMigration data = await _configMigrationRepository.GetConfigSettings(id);
                if (data == null)
                {
                    _logger.LogWarning("Migration setting not found for ID: {Id}", id);
                    return Enumerable.Empty<string>();
                }
                string val = data.Value;
                IEnumerable<string> retVal = val.Split(_separator);
                await _configMigrationRepository.DeleteConfigSettings(id);
                
                _logger.LogInformation("Successfully loaded and deleted migration setting with ID: {Id}", id);
                return retVal;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "Database error occurred while loading migration setting. ID: {Id}", id);
                throw new InvalidOperationException("Failed to load migration setting due to database error. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading migration setting. ID: {Id}", id);
                throw;
            }
        }
    }
}
