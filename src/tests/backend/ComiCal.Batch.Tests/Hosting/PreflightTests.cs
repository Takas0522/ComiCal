using ComiCal.Batch.Hosting;
using ComiCal.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ComiCal.Batch.Tests.Hosting;

public sealed class PreflightTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Execute_Preflight_RunsEveryCheckAndReportsFailures(int failure)
    {
        var checks = Substitute.For<IPreflightChecks>();
        Func<CancellationToken, Task>[] methods =
            [checks.CheckTokenAsync, checks.CheckPublicIpAsync, checks.CheckSecretsAsync, checks.CheckSqlAsync];
        for (var index = 0; index < methods.Length; index++)
        {
            methods[index](Arg.Any<CancellationToken>()).Returns(index == failure
                ? Task.FromException(new InvalidOperationException())
                : Task.CompletedTask);
        }
        var logger = Substitute.For<ILogger<Preflight>>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var runner = new BatchRunner(Substitute.For<ISingleInstanceGuard>(), new(() => Substitute.For<IStaleBatchRunCleaner>()),
            new(() => Substitute.For<IBatchRunRepository>()), new Preflight(checks, logger), Substitute.For<IBatchWork>(),
            Substitute.For<IFailureNotifier>(), Substitute.For<IHeartbeat>(), new ConfigurationBuilder().Build(),
            TimeProvider.System, NullLogger<BatchRunner>.Instance);

        Assert.Equal(failure < methods.Length ? ExitCodes.PreflightFailure : ExitCodes.Success,
            runner.Execute(new(BatchCommandKind.Preflight), TextWriter.Null, default));
        Assert.Equal(4, checks.ReceivedCalls().Count());
        Assert.Equal(failure < methods.Length ? 1 : 0,
            logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)));
    }
}
