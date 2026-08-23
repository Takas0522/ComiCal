using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace ComiCal.Infrastructure.Sql;

/// <summary>
/// Azure SQL の一時的（transient）エラーを判定する共通ヘルパー。
/// EF Core の <c>SqlServerRetryingExecutionStrategy</c> が transient とみなす
/// エラー番号と整合させることで、「EF Core 内部でリトライして力尽きた」場合と
/// 「そもそも即座に失敗した」場合の両方を同じ基準で 503 化できるようにする。
/// </summary>
public static class SqlTransientErrorClassifier
{
    // EF Core の既定 transient 番号のうち、Azure SQL Serverless の
    // auto-pause/auto-resume に起因して発生し得る代表的なものを含める。
    // 40613: Database is not currently available (auto-resume 中)
    // 40197/40501/40540: サービスがリクエストを処理できない (throttling/一時障害)
    // 4060: Cannot open database (auto-resume 中に発生することがある)
    // 49918/49919/49920: リソースガバナ関連の一時エラー
    // -2: SqlClient のタイムアウト (接続/コマンド)
    private static readonly HashSet<int> TransientErrorNumbers =
    [
        40613, 40197, 40501, 40540, 4060, 49918, 49919, 49920, -2,
    ];

    /// <summary>
    /// 例外チェーン（InnerException を含む）を辿り、Azure SQL の transient エラーに
    /// 起因するかどうかを判定する。クライアント切断由来の <see cref="OperationCanceledException"/>
    /// や、SQL 以外の要因（Blob/外部HTTP等）による <see cref="TimeoutException"/> は対象外とする
    /// （安易に 503 化すると「DB cold-start」以外の障害を誤って DB 起因と誤認させてしまうため、
    /// 安全側に倒し、SqlException の Number で判定できる場合のみ transient とみなす）。
    /// </summary>
    public static bool IsTransient(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is SqlException sqlEx && TransientErrorNumbers.Contains(sqlEx.Number))
                return true;

            // EF Core の EnableRetryOnFailure がリトライ上限に達すると
            // RetryLimitExceededException でラップされる。型自体は transient の根拠にならず、
            // inner を辿った先の SqlException 判定でカバーされるためスキップして続行する。
        }

        return false;
    }
}
