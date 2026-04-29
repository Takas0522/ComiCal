using ComiCal.Application.Common;
using ComiCal.Shared;

namespace ComiCal.Application.UseCases;

public sealed record GetHealthQuery();

public sealed record HealthStatus(string Status);

public interface IGetHealthUseCase
{
    System.Threading.Tasks.Task<Result<HealthStatus>> ExecuteAsync(
        GetHealthQuery query,
        UseCaseContext context,
        System.Threading.CancellationToken cancellationToken);
}

public sealed class GetHealthUseCase : IGetHealthUseCase
{
    public System.Threading.Tasks.Task<Result<HealthStatus>> ExecuteAsync(
        GetHealthQuery query,
        UseCaseContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);
        return System.Threading.Tasks.Task.FromResult(Result<HealthStatus>.Success(new HealthStatus("ok")));
    }
}
