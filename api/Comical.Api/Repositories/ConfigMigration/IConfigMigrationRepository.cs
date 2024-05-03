using Comical.Api.Models;
using System.Threading.Tasks;

namespace Comical.Api.Repositories
{
    public interface IConfigMigrationRepository
    {
        Task DeleteConfigSettings(string id);
        Task<ConfigMigration?> GetConfigSettings(string id);
        Task RegisterConfig(string id, string settings);
    }
}