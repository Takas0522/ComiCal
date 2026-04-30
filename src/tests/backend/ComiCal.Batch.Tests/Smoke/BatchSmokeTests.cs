using ComiCal.Batch.Triggers;
using Xunit;

namespace ComiCal.Batch.Tests.Smoke;

public sealed class BatchSmokeTests
{
    [Fact]
    public void Assembly_Loads()
        => Assert.NotNull(typeof(BatchTriggers).Assembly);
}
