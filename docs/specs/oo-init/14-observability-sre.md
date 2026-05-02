# 14. 監視 / SRE

## 14.1 KPI / メトリクス

### 14.1.1 ビジネス KPI

| KPI | 計測方法 |
|---|---|
| DAU / WAU | App Insights `customEvents` (`page.view`) のユニークユーザー集計（匿名は anonymousId、ログインは UserId hash）|
| 新規購読数 / 日 | API カスタムイベント `subscription.created` の集計 |
| リテンション (D7 / D30) | UserId / anonymousId の継続出現率 |
| 検索成功率 | `search.executed` に対する `search.result.zero` の比率 |

### 14.1.2 システムメトリクス

| メトリクス | 目標 |
|---|---|
| API p95 レイテンシ | **< 500ms** |
| API エラー率 | **< 1%**（超過でアラート）|
| バッチ成功率 | > 99% |
| バッチ取得件数 / 日 | 期待レンジ（過去 7 日 ±50%）外で警告 |
| サムネイルキャッシュヒット率 | > 80%（CoverHash 一致でスキップ）|
| SQL DTU / vCore 利用率 | < 70% |
| SWA SSR cold start 率 | 監視のみ |

## 14.2 アラートルール

| ルール | 条件 | 通知先 |
|---|---|---|
| バッチ失敗 | `BatchRuns.Status = Failed` または `failedItemCount >= 5` | Slack / Teams Webhook（Action Group）|
| API エラー率 | 5 分窓で `4xx + 5xx` 比率 > **1%** | Slack / Teams Webhook |
| SQL auto-pause 中の失敗増 | DB 接続失敗 5 件 / 5 分 | Slack / Teams Webhook |
| Application Insights 例外バースト | 30 件 / 5 分 | Slack / Teams Webhook |
| Function App ダウン | Availability < 99% / 15 分 | Slack / Teams Webhook |

## 14.3 ログ / トレース

- すべてのコンポーネントを **Application Insights + Log Analytics Workspace** に集約。
- W3C Trace Context (`traceparent`) を SSR → API → Batch → DB クエリまで伝播。
- 構造化ログのキー命名は `camelCase`（例: `volumeId`, `seriesId`）で統一。
- PII は記録しない（IdP Subject はハッシュ化）。

## 14.4 ダッシュボード

- Azure Workbook で以下を初期同梱:
  - **Daily Batch** Workbook: BatchRuns 履歴 / 取得件数推移 / FailedItems。
  - **API Health** Workbook: p50/p95/p99 / エラー率 / トップエンドポイント。
  - **User Activity** Workbook: DAU/WAU / 購読作成数 / 検索数。

## 14.5 性能目標（フロント）

| 指標 | 目標 |
|---|---|
| LCP | < 2.5s |
| TTFB | < 600ms |
| CLS | < 0.1 |

- Web Vitals を `web-vitals` ライブラリで測定し、`pageView` カスタムプロパティとして送信。
- Lighthouse CI を `pr-preview.yml` で実行し回帰検知。

## 14.6 キャッシュ戦略

- API: **ETag / If-None-Match** + SSR Transfer State。
- 静的アセット: SWA 標準のロングキャッシュ（content-hash ファイル名）。
- 表紙画像: Blob `Cache-Control: public, max-age=2592000, immutable`。

## 14.7 オンコール / 運用

- 個人 OSS のためオンコール体制は持たないが、アラート Webhook を Slack/Teams のオプトインチャネルに配送する想定。
- ランブック（バッチ失敗時の対応）は `.claude/skills/troubleshoot-batch` にテンプレート化。

## 14.8 サンプリング

- App Insights サンプリングは **既定 (適応サンプリング)**。
- 重要イベント (`subscription.created`, `purchase.updated`, `batch.*`) は `[Telemetry: Always]` 相当のフィルタで sampled-out させない。
