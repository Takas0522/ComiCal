using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comical.Api.Services
{
    public interface IConfigMigrationService
    {
        Task<IEnumerable<string>> LoadMigrationSetting(string id);
        Task<string> RegisterMigrationSetting(IEnumerable<string> value);
    }
}