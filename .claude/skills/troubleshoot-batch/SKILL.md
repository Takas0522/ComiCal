---
name: troubleshoot-batch
description: 'Use when diagnosing a Durable Functions batch incident: daily 03:00 JST collection failure, abnormal Rakuten Books API fetch counts, stalled thumbnail downloads, 429 rate-limit floods, non-deterministic orchestrator exceptions, or stuck instances. Walks through BatchRuns / FailedItems tables, Application Insights Kusto queries, Durable Functions instance state, and Storage Queue / DLQ inspection.'
argument-hint: '<yyyy-mm-dd of failed run>'
allowed-tools: Bash, Read
---

# troubleshoot-batch

## 診断フロー

### 1. 直近の BatchRun を特定

```sql
SELECT TOP 5 *
FROM dbo.BatchRuns
ORDER BY StartedAt DESC;
```

- `Status`、`StartedAt`、`CompletedAt`、`ErrorSummary` を確認
- 該当 RunId を控える

### 2. FailedItems を確認

```sql
SELECT *
FROM dbo.FailedItems
WHERE BatchRunId = '<RunId>'
ORDER BY FailedAt DESC;
```

- どのアイテム（ISBN / SeriesId）で失敗したか
- 失敗理由が rate limit / 4xx / 5xx / parse error のいずれか

### 3. Application Insights クエリ

```kusto
traces
| where timestamp > ago(24h)
| where customDimensions.RunId == "<RunId>"
| order by timestamp asc
| project timestamp, severityLevel, message, customDimensions
```

- Orchestrator → Activity の呼び出し系列を時系列で確認
- リトライ回数、最終失敗時のスタックトレース

### 4. Durable Functions の状態確認

- Azure Portal の Durable Functions UI で Orchestrator instance の状態確認
- 必要に応じて `Terminate` / `Rewind` / `Purge`
- CLI:
  ```bash
  func durable get-instances --connection-string-setting AzureWebJobsStorage
  ```

### 5. Storage Queue / DLQ

- `<taskhub>-control-XX` キューの滞留
- DLQ Storage Queue（カスタム）の毒メッセージ確認

## よくある原因と対処

| 症状 | 原因 | 対処 |
|------|------|------|
| 取得件数が 0 | 楽天 API 仕様変更 / applicationId 失効 | Key Vault シークレット確認、API レスポンス手動再現 |
| 大量 429 | RateLimiter 漏れ | `RakutenBooksClient` の RateLimitPolicy を確認、1 req/sec を超えていないか |
| サムネイル DL 停止 | 並列度過多 / Blob throttling | 並列度を 8 以下に、Blob のスケール確認 |
| Orchestrator が deterministic でない例外 | Orchestrator 内で `DateTime.Now` 等使用 | コードレビュー、`context.CurrentUtcDateTime` 等に置換 |
| 同じ ISBN で失敗継続 | データ異常（複数巻数表記等） | FailedItems の payload を確認、必要に応じ手動補正 |

## エスカレーション

- 復旧不能 / ユーザー影響あり → Slack #comical-alerts に Incident テンプレートで投稿
- データ復旧が必要なら DBA レビュー必須

## 関連

- `.github/instructions/backend-batch.instructions.md`
- `.github/instructions/backend-infrastructure.instructions.md`

## アンチパターン

- ❌ 失敗 instance を放置 (Purge せず制御キュー詰まり)
- ❌ 楽天 API への手動リトライをループで叩く（RateLimit 違反）
- ❌ 本番 DB を直接 UPDATE で修正（必ずスクリプト + レビュー）
