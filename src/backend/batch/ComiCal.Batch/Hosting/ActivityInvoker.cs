using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace ComiCal.Batch.Hosting;

public interface IActivityInvoker
{
    Task<TResult> RunAsync<TActivity, TInput, TResult>(
        TInput input, Func<TActivity, TInput, CancellationToken, Task<TResult>> run,
        CancellationToken ct, bool thumbnail = false) where TActivity : notnull;
}

public sealed class ActivityInvoker(IServiceScopeFactory scopeFactory, TimeProvider clock) : IActivityInvoker
{
    private readonly ResiliencePipeline _standard = CreatePipeline(clock, TimeSpan.FromSeconds(5));
    private readonly ResiliencePipeline _thumbnail = CreatePipeline(clock, TimeSpan.FromSeconds(3));

    public Task<TResult> RunAsync<TActivity, TInput, TResult>(
        TInput input, Func<TActivity, TInput, CancellationToken, Task<TResult>> run,
        CancellationToken ct, bool thumbnail = false) where TActivity : notnull
        => (thumbnail ? _thumbnail : _standard).ExecuteAsync(async token =>
        {
            token.ThrowIfCancellationRequested();
            await using var scope = scopeFactory.CreateAsyncScope();
            var activity = scope.ServiceProvider.GetRequiredService<TActivity>();
            return await run(activity, input, token);
        }, ct).AsTask();

    private static ResiliencePipeline CreatePipeline(TimeProvider clock, TimeSpan delay)
        => new ResiliencePipelineBuilder { TimeProvider = clock }
            .AddRetry(new RetryStrategyOptions
            {
                // The previous policy allowed three total attempts.
                MaxRetryAttempts = 2,
                Delay = delay,
                BackoffType = DelayBackoffType.Exponential,
                // 呼び出し元のキャンセルだけはリトライしない。HttpClient のタイムアウト（TaskCanceledException）は
                // 一時的な障害として旧 Durable のリトライポリシーと同様に再試行する。
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is { } ex
                    && !(ex is OperationCanceledException && args.Context.CancellationToken.IsCancellationRequested))
            })
            .Build();
}
