using ComiCal.Infrastructure.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComiCal.Batch.Triggers;

/// <summary>
/// Azure SQL Serverless の auto-pause からの復旧（auto-resume, 60〜120 秒程度）を
/// 日次バッチ本体の実行前に前倒しでキックしておく Warm-up 用 Timer Function。
///
/// スケジュールはアプリ設定 "WarmupBatchCronExpression"（NCRONTAB, UTC）で駆動しており、
/// 常に "DailyBatchCronExpression" の 10 分前になるよう Bicep 側でペアで設定する
/// （infra/modules/app.bicep のデフォルトは 17:50 UTC = 02:50 JST、日次バッチの 10 分前）。
/// この方式により、dev/prod でバッチ実行時刻をずらしたい場合もコード変更・再デプロイ不要で
/// Azure の環境変数（アプリ設定）変更のみで対応できる。
/// SQL の auto-resume に加えて Functions のコールドスタートや Timer の past-due 遅延も
/// 見込んだバッファとして 10 分を確保している（5 分では不足する可能性があるとのレビュー指摘を反映）。
///
/// 本 Function は Durable Orchestrator ではなく通常の Timer Function であるため、
/// backend-batch.instructions.md の Determinism ルール（DateTime.Now 禁止等）の対象外。
/// </summary>
public partial class WarmupTrigger(ComiCalDbContext dbContext, ILogger<WarmupTrigger> logger)
{
    [Function("WarmupBatchTimer")]
    public async Task RunAsync(
        [TimerTrigger("%WarmupBatchCronExpression%")] TimerInfo timerInfo,
        CancellationToken ct)
    {
        LogWarmupStarted(logger, timerInfo.IsPastDue);

        try
        {
            // DB を叩いて auto-resume をトリガーするだけが目的の軽量クエリ。
            // 結果自体は使わないため、存在確認 (AnyAsync) で十分。
            await dbContext.BatchRuns.AnyAsync(ct);
            LogWarmupSucceeded(logger);
        }
        catch (Exception ex)
        {
            // Warm-up の失敗は致命的ではない（本番バッチは別途 EnableRetryOnFailure で
            // auto-resume を吸収できるため）。ここで例外を伝播させて DailyFetchOrchestrator の
            // 実行に影響を与えないよう、ログのみ出力して握りつぶす。
            LogWarmupFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Warmup batch timer triggered. IsPastDue: {IsPastDue}")]
    private static partial void LogWarmupStarted(ILogger logger, bool isPastDue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Warmup query succeeded; database should be resumed.")]
    private static partial void LogWarmupSucceeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warmup query failed; database may still be resuming. This is non-fatal.")]
    private static partial void LogWarmupFailed(ILogger logger, Exception exception);
}
