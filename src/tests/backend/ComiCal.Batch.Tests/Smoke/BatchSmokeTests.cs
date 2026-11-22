using ComiCal.Batch.Jobs;
using Xunit;

namespace ComiCal.Batch.Tests.Smoke;

public sealed class BatchSmokeTests
{
    [Fact]
    public void Assembly_Loads()
        => Assert.NotNull(typeof(DailyFetchJob).Assembly);
}
