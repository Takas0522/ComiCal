using Comical.Api.Models;
using Comical.Api.Repositories;
using Comical.Api.Util.Common;
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
        private readonly string _separator = "[nextval]";

        public ConfigMigrationService(
            IConfigMigrationRepository configMigrationRepository
        )
        {
            _configMigrationRepository = configMigrationRepository;
        }

        public async Task<string> RegisterMigrationSetting(IEnumerable<string> value)
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
            return id;
        }

        public async Task<IEnumerable<string>> LoadMigrationSetting(string id)
        {
            ConfigMigration data = await _configMigrationRepository.GetConfigSettings(id);
            if (data == null)
            {
                return Enumerable.Empty<string>();
            }
            string val = data.Value;
            IEnumerable<string> retVal = val.Split(_separator);
            await _configMigrationRepository.DeleteConfigSettings(id);
            return retVal;

        }
    }
}
