using ComiCal.Domain.Entities;
using ComiCal.Domain.Repositories;
using Microsoft.DurableTask;

namespace ComiCal.Batch.Activities;

[DurableTask("CreateBatchRunActivity")]
public class CreateBatchRunActivity(IBatchRunRepository batchRunRepo)
    : TaskActivity<object?, Guid>
{
    public override async Task<Guid> RunAsync(TaskActivityContext context, object? _)
    {
        var batchRun = BatchRun.Create();
        return await batchRunRepo.CreateAsync(batchRun);
    }
}
