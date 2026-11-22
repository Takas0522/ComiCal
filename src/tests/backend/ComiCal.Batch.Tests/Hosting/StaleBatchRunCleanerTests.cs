using ComiCal.Batch.Hosting;
using ComiCal.Domain.Entities;
using ComiCal.Domain.Enums;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class StaleBatchRunCleanerTests
{
    [Fact]
    public async Task CleanAsync_StaleRuns_FinalizesAndPersistsEachAsFailed()
    {
        var repository = Substitute.For<IBatchRunRepository>();
        var stale = new[] { BatchRun.Create(), BatchRun.Create() };
        var cutoff = DateTime.UtcNow;
        repository.FindStaleAsync(cutoff, default).Returns(stale);
        var cleaner = new StaleBatchRunCleaner(repository, NullLogger<StaleBatchRunCleaner>.Instance);

        await cleaner.CleanAsync(cutoff, default);

        foreach (var run in stale)
        {
            Assert.Equal(BatchRunStatus.Failed, run.Status);
            Assert.Equal(1, run.FailedItemCount);
            Assert.NotNull(run.CompletedAt);
            await repository.Received(1).UpdateAsync(run);
        }
    }
}
