using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.Azure.Functions.Worker;

namespace ComiCal.Batch.Activities;

public class CreateBatchRunActivity(IBatchRunRepository batchRunRepo)
{
    [Function("CreateBatchRunActivity")]
    public async Task<Guid> Run([ActivityTrigger] object? _)
    {
        var batchRun = BatchRun.Create();
        return await batchRunRepo.CreateAsync(batchRun);
    }
}
