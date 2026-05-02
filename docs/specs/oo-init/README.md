# ComiCal 初期仕様書 (oo-init)

本ディレクトリは `docs/init.md` を原典とし、ComiCal（UI 表示名: **まんがリマインダー**）のビジネス仕様とシステム仕様をセクション単位に分割して整理したものです。すべての仕様は **2026 年 4 月時点** を「latest」として扱います。

## 構成

### ビジネス仕様 (What / Why)

| # | ファイル | 内容 |
|---|---|---|
| 01 | [01-business-overview.md](./01-business-overview.md) | プロジェクト概要・提供価値・ステークホルダー |
| 02 | [02-target-and-scope.md](./02-target-and-scope.md) | ターゲット / ロケール / MVP スコープ |
| 03 | [03-functional-requirements.md](./03-functional-requirements.md) | 機能要件（購読・購入・検索・カレンダー）|
| 04 | [04-ui-ux-spec.md](./04-ui-ux-spec.md) | UI / UX / アクセシビリティ仕様 |
| 05 | [05-discovery-sharing.md](./05-discovery-sharing.md) | ディスカバリ・共有方針（MVP 外）|
| 19 | [19-oss-legal.md](./19-oss-legal.md) | OSS / 法務 / アフィリエイト規約 |
| 21 | [21-future-extensions.md](./21-future-extensions.md) | 将来拡張候補（非 MVP）|

### システム仕様 (How)

| # | ファイル | 内容 |
|---|---|---|
| 06 | [06-architecture-overview.md](./06-architecture-overview.md) | 技術スタック・論理構成図・環境 |
| 07 | [07-domain-model.md](./07-domain-model.md) | ドメインモデル / 集約ルール |
| 08 | [08-database-spec.md](./08-database-spec.md) | DB スキーマ / 検索 / SSDT |
| 09 | [09-rakuten-and-batch.md](./09-rakuten-and-batch.md) | 楽天 Books API 連携 / Durable Functions バッチ |
| 10 | [10-frontend-spec.md](./10-frontend-spec.md) | Angular v21 / Tailwind v4 / SSR |
| 11 | [11-auth-session.md](./11-auth-session.md) | 認証 / セッション / 匿名 ⇄ ログイン |
| 12 | [12-backend-api.md](./12-backend-api.md) | Azure Functions API（Clean Architecture）|
| 13 | [13-infrastructure.md](./13-infrastructure.md) | Azure / Bicep IaC |
| 14 | [14-observability-sre.md](./14-observability-sre.md) | KPI / アラート / 性能目標 |
| 15 | [15-security.md](./15-security.md) | OWASP / シークレット / データ保持 |
| 16 | [16-test-strategy.md](./16-test-strategy.md) | テスト戦略 / E2E POM ルール |
| 17 | [17-cicd.md](./17-cicd.md) | GitHub Actions / Feature Flag / リリース |
| 18 | [18-devcontainer.md](./18-devcontainer.md) | DevContainer / 開発体験 |
| 20 | [20-repo-structure.md](./20-repo-structure.md) | モノレポディレクトリ構造 |

## 用語

| 用語 | 定義 |
|---|---|
| ComiCal | プロジェクトコード名 / リポジトリ名 |
| まんがリマインダー | UI 表示名（エンドユーザー向け）|
| シリーズ | `(NormalizedSeriesName, PrimaryAuthor)` で集約された漫画作品 |
| 巻 (Volume) | ISBN-13 で一意に識別される個別の単行本 |
| 購読 (Subscription) | ユーザーがシリーズを「読みたい」として登録した状態 |
| 購入 (Purchase) | ユーザーが特定の巻に対して持つ状態（未購入 / 購入済 / 読了 / 予約中）|
| latest | 2026 年 4 月時点の最新安定版 |

## 参照

- 原典: [`docs/init.md`](../../init.md)
- レイヤー別実装ルール: [`.github/instructions/`](../../../.github/instructions/)
- Skill 定義: [`.claude/skills/`](../../../.claude/skills/)
