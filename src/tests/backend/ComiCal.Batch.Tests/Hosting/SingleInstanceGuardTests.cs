using ComiCal.Batch.Hosting;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public async Task RunExclusive_HeldByAnotherThread_ReturnsAlreadyRunningAndReleasesAfterWork()
    {
        var guard = new SingleInstanceGuard();
        using var release = new ManualResetEventSlim();
        var acquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = Task.Run(() => guard.RunExclusive(() =>
        {
            acquired.SetResult();
            return release.Wait(TimeSpan.FromSeconds(10)) ? ExitCodes.Success : ExitCodes.Timeout;
        }));

        try
        {
            await acquired.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(ExitCodes.AlreadyRunning, guard.RunExclusive(() => ExitCodes.Success));
        }
        finally
        {
            release.Set();
        }

        Assert.Equal(ExitCodes.Success, await owner);
        Assert.Equal(ExitCodes.Success, guard.RunExclusive(() => ExitCodes.Success));
    }
}
