namespace ComiCal.Batch.Hosting;

public interface ISingleInstanceGuard
{
    int RunExclusive(Func<int> action);
}

public sealed class SingleInstanceGuard : ISingleInstanceGuard
{
    public int RunExclusive(Func<int> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var mutex = new Mutex(false, @"Global\ComiCal.Batch");
        try
        {
            if (!mutex.WaitOne(0))
                return ExitCodes.AlreadyRunning;
        }
        catch (AbandonedMutexException)
        {
            // The waiting thread acquires ownership when the previous owner has exited.
        }

        try
        {
            return action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
