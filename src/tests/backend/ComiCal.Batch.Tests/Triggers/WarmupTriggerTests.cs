using ComiCal.Batch.Triggers;
using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ComiCal.Batch.Tests.Triggers;

public sealed class WarmupTriggerTests
{
    [Fact]
    public async Task RunAsync_WhenDatabaseUnreachable_DoesNotThrow()
    {
        // Arrange: 到達不能な接続文字列（ConnectTimeout を短くして高速に失敗させる）で
        // DbContext を構成し、DB が resume 中/到達不能な状況を模擬する。
        var options = new DbContextOptionsBuilder<ComiCalDbContext>()
            .UseSqlServer(
                "Server=tcp:unreachable-host-for-test.invalid,1433;Database=test;Connect Timeout=1;Encrypt=True;TrustServerCertificate=True;")
            .Options;
        using var dbContext = new ComiCalDbContext(options);
        var logger = new RecordingLogger<WarmupTrigger>();
        var sut = new WarmupTrigger(dbContext, logger);
        var timerInfo = new TimerInfo();

        // Act & Assert: 例外が伝播しない（握りつぶされる）こと。
        var exception = await Record.ExceptionAsync(() => sut.RunAsync(timerInfo, CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task RunAsync_WhenDatabaseUnreachable_LogsWarning()
    {
        var options = new DbContextOptionsBuilder<ComiCalDbContext>()
            .UseSqlServer(
                "Server=tcp:unreachable-host-for-test.invalid,1433;Database=test;Connect Timeout=1;Encrypt=True;TrustServerCertificate=True;")
            .Options;
        using var dbContext = new ComiCalDbContext(options);
        var logger = new RecordingLogger<WarmupTrigger>();
        var sut = new WarmupTrigger(dbContext, logger);
        var timerInfo = new TimerInfo();

        await sut.RunAsync(timerInfo, CancellationToken.None);

        // Warning レベルでログが出力されること（DailyFetchOrchestrator へは影響させないが、
        // 運用上の可観測性のため記録する）。
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    /// <summary>
    /// ILogger&lt;T&gt; の呼び出し（LoggerMessage ソース生成含む）を記録するだけの簡易フェイク。
    /// NSubstitute で LoggerMessage 生成コードの Log 呼び出しを検証するのは実装依存で壊れやすいため、
    /// 実際に呼ばれたログレベルだけを素朴に記録する方式にしている。
    /// </summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}

