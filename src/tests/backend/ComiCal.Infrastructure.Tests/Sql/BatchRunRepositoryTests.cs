using ComiCal.Domain.Enums;
using ComiCal.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComiCal.Infrastructure.Tests.Sql;

[Collection(nameof(SqlRepositoryFixtureGroup))]
public sealed class BatchRunRepositoryTests(PrefixSearchFixture fixture)
{
    [Fact]
    public async Task Queries_ExistingRuns_ApplyStatusAndHalfOpenUtcBounds()
    {
        await using var db = fixture.CreateContext();
        var repository = new BatchRunRepository(db);
        var start = new DateTime(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var stale = Guid.NewGuid();
        var success = Guid.NewGuid();
        var recent = Guid.NewGuid();
        var partial = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.BatchRuns (BatchRunId, StartedAt, CompletedAt, Status)
            VALUES ({stale}, {start.AddDays(-1)}, NULL, N'Running'),
                ({success}, {start.AddHours(-1)}, {start}, N'Succeeded'),
                ({recent}, {start}, NULL, N'Running'),
                ({partial}, {start.AddHours(1)}, {start.AddHours(2)}, N'PartialFailure'),
                (NEWID(), {end}, {end}, N'Succeeded');
            """);

        var run = Assert.Single(await repository.FindStaleAsync(start));
        Assert.Equal(stale, run.BatchRunId);
        run.Complete(0, 0, 0, 0, 1);
        await repository.UpdateAsync(run);
        db.ChangeTracker.Clear();

        Assert.Empty(await repository.FindStaleAsync(start));
        var failed = await db.BatchRuns.SingleAsync(item => item.BatchRunId == stale);
        Assert.Equal(BatchRunStatus.Failed, failed.Status);
        Assert.NotNull(failed.CompletedAt);
        Assert.Equal(1, failed.FailedItemCount);
        Assert.True(await repository.HasSucceededAsync(start, end));
        Assert.False(await repository.HasSucceededAsync(start.AddDays(-1), start));
        Assert.False(await repository.HasSucceededAsync(start.AddSeconds(1), end));

        var started = await repository.ListStartedAsync(start, end);
        Assert.Equal(new[] { partial, recent }, started.Select(item => item.BatchRunId));
        Assert.Equal(BatchRunStatus.PartialFailure, started[0].Status);
    }
}
