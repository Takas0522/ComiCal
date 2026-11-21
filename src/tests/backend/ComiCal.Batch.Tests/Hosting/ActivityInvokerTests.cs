using ComiCal.Batch.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class ActivityInvokerTests
{
    [Fact]
    public async Task RunAsync_SeparateCalls_UseSeparateDisposedScopes()
    {
        await using var services = CreateServices();
        var invoker = new ActivityInvoker(services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System);

        var first = await invoker.RunAsync<ScopedActivity, int, ScopedActivity>(
            0, static (activity, _, _) => Task.FromResult(activity), CancellationToken.None);
        var second = await invoker.RunAsync<ScopedActivity, int, ScopedActivity>(
            0, static (activity, _, _) => Task.FromResult(activity), CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.True(first.Disposed);
        Assert.True(second.Disposed);
    }

    [Theory]
    [InlineData(false, 5)]
    [InlineData(true, 3)]
    public async Task RunAsync_PersistentFailure_MakesThreeAttemptsWithExponentialBackoff(bool thumbnail, int initialDelay)
    {
        await using var services = CreateServices();
        var clock = new RecordingTimeProvider();
        var invoker = new ActivityInvoker(services.GetRequiredService<IServiceScopeFactory>(), clock);
        var attempts = new List<ScopedActivity>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoker.RunAsync<ScopedActivity, int, int>(0, (activity, _, _) =>
            {
                attempts.Add(activity);
                throw new InvalidOperationException();
            }, CancellationToken.None, thumbnail));

        Assert.Equal(3, attempts.Distinct().Count());
        Assert.All(attempts, activity => Assert.True(activity.Disposed));
        Assert.Equal([TimeSpan.FromSeconds(initialDelay), TimeSpan.FromSeconds(initialDelay * 2)], clock.Delays);
    }

    [Fact]
    public async Task RunAsync_CallerCancellation_IsNotRetried()
    {
        await using var services = CreateServices();
        var clock = new RecordingTimeProvider();
        var invoker = new ActivityInvoker(services.GetRequiredService<IServiceScopeFactory>(), clock);
        using var cts = new CancellationTokenSource();
        var attempts = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            invoker.RunAsync<ScopedActivity, int, int>(0, (_, _, token) =>
            {
                attempts++;
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            }, cts.Token));

        Assert.Equal(1, attempts);
        Assert.Empty(clock.Delays);
    }

    [Fact]
    public async Task RunAsync_TimeoutWithoutCallerCancellation_IsRetried()
    {
        await using var services = CreateServices();
        var clock = new RecordingTimeProvider();
        var invoker = new ActivityInvoker(services.GetRequiredService<IServiceScopeFactory>(), clock);
        var attempts = 0;

        var result = await invoker.RunAsync<ScopedActivity, int, int>(0, (_, _, _) =>
        {
            attempts++;
            // HttpClient のタイムアウトは呼び出し元がキャンセルしていなくても TaskCanceledException になる
            return attempts < 3 ? throw new TaskCanceledException("HttpClient timeout") : Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
    }

    private static ServiceProvider CreateServices()
        => new ServiceCollection().AddScoped(_ => new ScopedActivity()).BuildServiceProvider();

    private sealed class ScopedActivity : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingTimeProvider : TimeProvider
    {
        public List<TimeSpan> Delays { get; } = [];

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Delays.Add(dueTime);
            return TimeProvider.System.CreateTimer(callback, state, TimeSpan.Zero, period);
        }
    }
}
