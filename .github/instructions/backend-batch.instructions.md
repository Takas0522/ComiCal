---
description: 'Use when implementing or reviewing .NET batch jobs and activities, Rakuten Books API rate limiting (1 request per 5 seconds), or batch retry and cancellation under src/backend/batch/.'
applyTo: 'src/backend/batch/**'
---

# Backend Batch Instructions

## ランタイムと構成

- .NET 10 Generic Host のコンソールアプリ。メンテナーの Windows PC で `tools/publish-batch.ps1` により配置し、`tools/register-batch-task.ps1` で毎日 03:00 のタスクを登録する。手動起動は配置先の `ComiCal.Batch.exe run`。詳細は[運用手順](../../docs/runbooks/batch-operations.md)。
- `Hosting/BatchRunner` が排他、事前チェック、Stale 掃除、JST 当日分の成功確認、ウォームアップ、終了コードと通知を扱う。
- `Jobs/DailyFetchJob` が実行履歴を作り、`FetchPagesJob` の直列ページ収集と `ThumbnailJob` の並列取得（最大 8）を実行して結果を確定する。
- `Activities/` は SQL、HTTP、Blob などの I/O を担う。各呼び出しは `Hosting/ActivityInvoker` が作る独立した DI スコープで実行する。

## Activity の追加

1. `Activities/` にクラスを追加し、依存サービスをコンストラクターで受け取る。入出力と `CancellationToken` を受ける `Run(input, ct)` を定義する。
2. `Hosting/BatchServiceCollectionExtensions.cs` の `AddBatchInfrastructure` で `AddScoped<XxxActivity>()` を登録する。
3. Job で `IActivityInvoker.RunAsync<XxxActivity, XxxInput, XxxOutput>(input, static (activity, args, token) => activity.Run(args, token), ct)` を呼ぶ。
4. ISBN の UPSERT、画像ハッシュ比較など、再実行しても重複しない処理にする。トークンを I/O まで渡し、ログには実行 ID や対象キーを残す。資格情報はログに含めない。

ActivityInvoker は例外を指数バックオフで合計最大 3 回試行する。初回の待機は通常 5 秒、サムネイル用の `thumbnail: true` は 3 秒。呼び出し元のキャンセルは再試行しない。

## 収集と運用

- 楽天 BooksBookSearch / 20170404 の `booksGenreId = 001001` を使う。楽天へのリクエストは既存の `SlidingWindowRateLimiter` で 5 秒に 1 回。資格情報は Key Vault から取得する。
- 月のみの発売日はその月の末日と `ReleaseDateIsMonthOnly = true` を保存する。未定は `null`。
- 中断時は次回最初から処理し、古い Running は Stale 掃除で Failed にする。一部失敗で完走した場合は警告と終了コード 0、`BatchRuns.Status` は Failed。当日の Succeeded がない場合、手動の `run` も全件処理する。
- 実行履歴と失敗の詳細は `BatchRuns` / `FailedItems` とローカルログで確認する。通知は `run` の結果を対象とし、失敗 Webhook は Slack / Teams 互換の `text` を含む。
