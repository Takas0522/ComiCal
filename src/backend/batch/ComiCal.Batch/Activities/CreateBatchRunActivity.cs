using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;

namespace ComiCal.Batch.Activities;

public class CreateBatchRunActivity(IBatchRunRepository batchRunRepo)
{
    public Task<Guid> Run(object? _) => Run(_, CancellationToken.None);

    public async Task<Guid> Run(object? _, CancellationToken ct)
    {
        var batchRun = BatchRun.Create();
        return await batchRunRepo.CreateAsync(batchRun, ct);
    }
}
