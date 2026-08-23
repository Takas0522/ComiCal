using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using ComiCal.Infrastructure.Sql;
using Xunit;

namespace ComiCal.Infrastructure.Tests.Sql;

public sealed class SqlTransientErrorClassifierTests
{
    [Theory]
    [InlineData(40613)] // Database is not currently available (auto-resume 中)
    [InlineData(40197)] // The service has encountered an error processing your request
    [InlineData(40501)] // The service is currently busy
    [InlineData(40540)] // The service has encountered an error processing your request
    [InlineData(4060)]  // Cannot open database
    [InlineData(49918)] // Cannot process request. Not enough resources to process request
    [InlineData(49919)] // Cannot process create or update request
    [InlineData(49920)] // Cannot process request. Too many operations in progress
    [InlineData(-2)]    // SqlClient timeout
    public void IsTransient_KnownTransientSqlErrorNumbers_ReturnsTrue(int errorNumber)
    {
        var sqlException = CreateSqlException(errorNumber);

        Assert.True(SqlTransientErrorClassifier.IsTransient(sqlException));
    }

    [Fact]
    public void IsTransient_NonTransientSqlErrorNumber_ReturnsFalse()
    {
        // 2627: Violation of PRIMARY KEY constraint — 恒久的なエラーであり transient ではない。
        var sqlException = CreateSqlException(2627);

        Assert.False(SqlTransientErrorClassifier.IsTransient(sqlException));
    }

    [Fact]
    public void IsTransient_RetryLimitExceededExceptionWrappingTransientSqlException_ReturnsTrue()
    {
        var sqlException = CreateSqlException(40613);
        var wrapped = new RetryLimitExceededException("retry limit exceeded", sqlException);

        Assert.True(SqlTransientErrorClassifier.IsTransient(wrapped));
    }

    [Fact]
    public void IsTransient_PlainTimeoutException_ReturnsFalse()
    {
        // SQL 起因と断定できない TimeoutException（Blob/外部HTTP等の可能性もある）を
        // 一律 transient 扱いすると、DB cold-start 以外の障害まで「起動中」と誤認させてしまう。
        // SqlException の Number で判定できる場合のみ transient とみなす（安全側に倒す）。
        var timeout = new TimeoutException("Command timeout expired");

        Assert.False(SqlTransientErrorClassifier.IsTransient(timeout));
    }

    [Fact]
    public void IsTransient_RetryLimitExceededExceptionWrappingNonTransientTimeoutException_ReturnsFalse()
    {
        // RetryLimitExceededException でラップされていても、inner が SqlException 以外なら
        // transient 判定はしない。
        var timeout = new TimeoutException("some other timeout");
        var wrapped = new RetryLimitExceededException("retry limit exceeded", timeout);

        Assert.False(SqlTransientErrorClassifier.IsTransient(wrapped));
    }

    [Fact]
    public void IsTransient_OperationCanceledException_ReturnsFalse()
    {
        // クライアント切断由来のキャンセルと区別できないため、安全側に倒して対象外とする。
        var cancelled = new OperationCanceledException("The operation was canceled.");

        Assert.False(SqlTransientErrorClassifier.IsTransient(cancelled));
    }

    [Fact]
    public void IsTransient_UnrelatedException_ReturnsFalse()
    {
        var ex = new InvalidOperationException("some other error");

        Assert.False(SqlTransientErrorClassifier.IsTransient(ex));
    }

    /// <summary>
    /// SqlException のコンストラクタは internal のため、テストではリフレクションを用いて
    /// 指定した Number を持つインスタンスを生成する。Microsoft.Data.SqlClient の内部実装に
    /// 依存するテストヘルパーであり、パッケージのメジャーアップデート時に壊れる可能性がある点に留意。
    /// </summary>
    private static SqlException CreateSqlException(int number)
    {
        var sqlErrorCollectionType = typeof(SqlErrorCollection);
        var errorCollection = (SqlErrorCollection)Activator.CreateInstance(
            sqlErrorCollectionType, nonPublic: true)!;

        var sqlErrorType = typeof(SqlError);
        var errorCtor = sqlErrorType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(uint), typeof(Exception)],
            null)
            ?? sqlErrorType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)],
                null);

        object sqlError = errorCtor!.GetParameters().Length == 9
            ? errorCtor.Invoke([number, (byte)0, (byte)0, "server", "error message", "proc", 0, (uint)0, null])
            : errorCtor.Invoke([number, (byte)0, (byte)0, "server", "error message", "proc", 0, null]);

        sqlErrorCollectionType
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(errorCollection, [sqlError]);

        return (SqlException)typeof(SqlException)
            .GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic,
                null, [typeof(SqlErrorCollection), typeof(string)], null)!
            .Invoke(null, [errorCollection, "11.0.0"])!;
    }
}
