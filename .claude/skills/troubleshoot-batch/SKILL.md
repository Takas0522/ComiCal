---
name: troubleshoot-batch
description: 'Use when diagnosing the Windows daily Batch using local logs, task results, status, BatchRuns and FailedItems, or external heartbeat alerts.'
argument-hint: '<yyyy-mm-dd of failed run>'
allowed-tools: Bash, Read
---

# troubleshoot-batch

1. Windows タスク スケジューラの `ComiCal-DailyBatch` で有効状態、履歴、前回の実行結果を確認する。ログが無ければ PC の稼働、実行ユーザー、ログオン状態、スリープ解除設定を調べる。
2. `%LOCALAPPDATA%\ComiCal\Batch\logs\batch-YYYYMMDD.log`（または `Batch:LogDirectory`）を確認する。配置済み exe で `status` または `status --date yyyy-MM-dd` を実行し、JST の対象日と照合する。
3. `/admin/batch-runs` または SQL の `BatchRuns` で状態と件数を確認する。`FailedItems` を `BatchRunId` で絞り、`ItemKey` と `Reason` をログに突き合わせる。Running が残った場合はプロセスの有無を確認する。閾値を過ぎた記録は次回 `run` で Failed になる。
4. 終了コードは 0 が成功・成功済み・一部失敗で完走、1 が処理失敗、2 が事前チェック失敗または引数不正、3 がタイムアウト、4 が多重起動。0 でも警告と Status が Failed なら項目別に調べる。2 は `preflight` とログで Entra トークン・IP・Key Vault・SQL のどこが失敗したかを特定する。楽天の 403 は許可 IP と資格情報、429 は同じ楽天アプリを使う他の実行も調べる。
5. ハートビート期限切れ時はタスク履歴と監視サービスの受信履歴を確認する。実行済みなら `Heartbeat could not be delivered` をログで探す。通知は `run` の結果だけが対象。コード 0 は成功 ping、1〜3 は `/fail` と失敗 Webhook を送る。
6. 原因解消後に `run` を実行して結果を確認する。途中再開はなく、同じ JST 日に Succeeded がなければ全件処理する。成功済みの日の明示的な再実行では `run --force` を使う。

初期設定、IP 更新と終了コード別の対処は[運用手順](../../../docs/runbooks/batch-operations.md)を参照する。
