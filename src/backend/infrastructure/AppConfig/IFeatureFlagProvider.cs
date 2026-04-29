using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ComiCal.Infrastructure.AppConfig;

public interface IFeatureFlagProvider
{
    Task<bool> IsEnabledAsync(string featureKey, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, bool>> GetAllAsync(CancellationToken cancellationToken);
}
