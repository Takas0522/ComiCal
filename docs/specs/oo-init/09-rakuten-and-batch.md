# 09. 楽天 Books API 連携 / バッチ仕様

> **2026-11 改訂（[ADR 0004](../../adr/0004-azure-cost-reduction.md)）**: 日次収集を Windows コンソールアプリに移行しました。
>
> - Batch はメンテナー PC で 配置済みの `ComiCal.Batch.exe run` で実行（Windows タスク スケジューラで毎日 03:00）。楽天アプリの許可 IP は自宅のグローバル IP。
> - API の楽天フォールバック検索 / 楽天候補からの購読は無効（D-1）。

## 9.1 楽天 Books API

| 項目 | 値 |
|---|---|
| エンドポイント | **BooksBookSearch / 20170404** |
| ジャンル | `booksGenreId = 001001` (コミック) に限定 |
| 認証 | Batch 実行ユーザーの資格情報で Key Vault から楽天の Application ID・Access Key・Affiliate ID を取得 |
| レートリミット | **5 秒 1 リクエスト以下**（楽天ウェブサービス側の Application 設定「Expected QPS: 5」は「5 秒に 1 回」を意味し、「1 秒に 5 回」ではない点に注意）|
| ページサイズ | 30 件（API 上限）|
| ソート | `-releaseDate`（降順） |
| 取得期間 | Job は前後 6 ヶ月を渡す。楽天の検索リクエストに日付条件は送らず、現行 Batch は返却ページを最大 100 ページまで処理する|

### 9.1.1 レートリミッター

`SlidingWindowRateLimiter` が 5 秒に 1 回のペースを制御する。ページ取得の例外は `ActivityInvoker` の指数バックオフで最大 3 試行する。

### 9.1.2 API レスポンス → ドメインマッピング

| 楽天フィールド | ComiCal フィールド |
|---|---|
| `isbn` | `Volumes.Isbn13`（13 桁検証）|
| `title` | `Series.Title` 抽出 + `Volumes.VolumeNumber` 抽出 |
| `author` | `Authors.Name`（カンマ / ／ で分割）|
| `publisherName` | `Publishers.Name` |
| `salesDate` | `Volumes.ReleaseDate` + `ReleaseDateIsMonthOnly` 判定 |
| `largeImageUrl` | サムネイル取得元 |
| `itemUrl` | `Volumes.RakutenItemUrl` |

## 9.2 日次 Batch

### 9.2.1 起動と当日判定

Windows タスク スケジューラが毎日 03:00 JST に `ComiCal.Batch.exe run` を起動する。手動でも同じコマンドを使う。当日に開始した Succeeded があれば収集をスキップし、`run --force` で全件再実行できる。

### 9.2.2 Job と Activity

1. `BatchRunner` が排他、事前チェック、Stale 掃除、当日判定と SQL ウォームアップを行う。
2. `DailyFetchJob` が実行履歴を作り、`FetchPagesJob` が各ページを直列に取得して ISBN で UPSERT する。
3. `ThumbnailJob` が画像を最大 8 並列で取得し、画像ハッシュが変わった場合に Blob と `ThumbnailAssets` を更新する。
4. `DailyFetchJob` が件数を集計して `BatchRuns` を確定する。各 Activity の呼び出しには `IActivityInvoker` を使い、試行ごとに独立した DI スコープを作る。

### 9.2.3 障害と再実行

- ISBN の UPSERT と画像ハッシュ比較によって再実行をべき等にする。中断後は先頭から実行する。閾値を過ぎた Running は次回の Stale 掃除で Failed になる。
- 一部項目に失敗しても最後まで処理した場合は終了コード 0 と警告ログを出し、履歴の Status は Failed になる。当日 Succeeded がない場合、手動の `run` でも失敗項目を含む全件を再処理する。
- 項目別の失敗情報は `FailedItems` とローカルログで確認する。

## 9.3 サムネイル仕様

| 項目 | 値 |
|---|---|
| 並列度 | **8** |
| 形式 | 楽天が返す元画像（JPEG）+ オプションで AVIF/WebP 変換（将来）|
| Blob コンテナ | `covers`（public read）|
| キー | `covers/{sha256}.jpg` |
| Cache-Control | `public, max-age=2592000, immutable` |

## 9.4 バッチ実行履歴

`BatchRuns` テーブルに以下を記録:

| カラム | 説明 |
|---|---|
| `BatchRunId` | GUID |
| `StartedAt` / `CompletedAt` | UTC |
| `Status` | `Running` / `Succeeded` / `Failed` / `Cancelled` |
| `FetchedItemCount` | 楽天から取得したアイテム数 |
| `UpsertedVolumeCount` | UPSERT 実行件数 |
| `DownloadedThumbnailCount` | 実際に DL したサムネ数 |
| `SkippedThumbnailCount` | CoverHash 一致でスキップ |
| `FailedItemCount` | 失敗件数 |

## 9.5 観測性

実行結果は `status`、管理画面 `/admin/batch-runs`、ローカルの構造化ログで確認する。外部ハートビートと失敗 Webhook は `run` の終了結果から送信し、Webhook は Slack / Teams 互換の `text` を含む。設定と障害対応は[運用手順](../../runbooks/batch-operations.md)を参照。
