---
description: 'Use when implementing or reviewing Durable Functions orchestrators/activities, scheduled batch jobs, Rakuten Books API rate limiting (1 req/sec), retry policies, fan-out/fan-in patterns, or orchestrator determinism rules under src/backend/batch/.'
applyTo: 'src/backend/batch/**'
---

# Backend Batch (Durable Functions) Instructions

## ランタイム

- **.NET 10 Isolated Worker** + **Durable Functions** + **Consumption Plan**
- スケジュール: **毎日 03:00 JST フル収集**（Timer Trigger）
- 手動起動: 管理者用 HTTP Trigger（Function Key + Entra 保護）

## Determinism（Orchestrator のルール）

Orchestrator 関数は **決定的** であること:

- ❌ `DateTime.Now` / `DateTimeOffset.Now` / `Random` / `Guid.NewGuid()` 使用禁止
- ❌ 直接 I/O（HTTP 呼び出し、DB アクセス、ファイル I/O）禁止
- ✅ 現在時刻が必要な場合は `context.CurrentUtcDateTime`
- ✅ I/O は Activity 関数に委譲

## パターン

### Function Chaining（楽天 API ページ収集）
- ページ単位で Activity を **直列に await**
- スキャン期間: これから 6 ヶ月先まで（初回投入時は 6 ヶ月前も含む）

### Fan-out / Fan-in（サムネイル取得）
- `Task.WhenAll(...)` で並列実行
- **並列度 8**（セマフォと反応を見て調整）
- 大規模並列時は Sub-Orchestrator に分割

```csharp
[Function(nameof(ThumbnailOrchestrator))]
public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
{
    var volumeIds = await context.CallActivityAsync<List<Guid>>(nameof(GetPendingVolumesActivity), null);
    var tasks = volumeIds.Select(id => context.CallActivityAsync(nameof(FetchThumbnailActivity), id));
    await Task.WhenAll(tasks);
}
```

## リトライ・冪等性

- **Activity はべき等に設計**（再実行時の二重作成を防ぐ）
- ISBN を主軸に **UPSERT**、表紙の **hash** で同一性判定し再ダウンロードスキップ
- リトライポリシー:
  ```csharp
  var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
      maxNumberOfAttempts: 5,
      firstRetryInterval: TimeSpan.FromSeconds(5),
      backoffCoefficient: 2.0));
  ```
- 失敗時: **DLQ (Storage Queue)** + **Application Insights アラート**

## 楽天 Books API 制約

- エンドポイント: **BooksBookSearch / 20170404**
- ジャンル: `booksGenreId = 001001`（コミック）
- **レートリミット: 1 秒 1 リクエスト以下** — クライアント側に **RateLimiter ポリシー** 必須（`System.Threading.RateLimiting` または Polly）
- `applicationId` は Key Vault 参照経由（Managed Identity）
- 巻数は楽天 API のタイトルから正規表現抽出 → 管理画面で手動修正可

## 発売日処理

- 「月のみ」の場合は **その月の末日**を保存し `ReleaseDateIsMonthOnly = true` フラグを立てる
- 「未定」は `null` を許容し UI で「未定」と表示

## ロギング・監視

- バッチ実行履歴は `BatchRuns` テーブル、失敗アイテムは `FailedItems` に記録
- バッチ失敗 → **Slack / Teams Webhook 通知**
- Application Insights のカスタムメトリクス: 取得件数 / サムネイルキャッシュヒット率 / バッチ成功率

## アンチパターン

- ❌ Orchestrator 内で `DateTime.Now` / `Random` / 直接 I/O
- ❌ Activity が非べき等
- ❌ 楽天 API レートリミットを無視（1 req/sec 超過）
- ❌ サムネイル並列度を 8 を大きく超える設定
- ❌ Activity 関数間で状態を共有（Orchestrator 経由でデータを渡す）
