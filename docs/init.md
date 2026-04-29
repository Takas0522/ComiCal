# まんがリマインダー (ComiCal)

> 楽天Books APIから漫画の発売情報を集約し、ユーザーの「読みたい」「買った」を一元管理する Web アプリケーション。
> プロジェクトコード名 / リポジトリ名: **ComiCal**　 / 　 UI 表示名: **まんがリマインダー**

---

## 1. 提供する価値 (What)

- 楽天 Books API から定期的に漫画の発売情報を取得し提供する。
- ユーザーは購読したい漫画シリーズを登録し、直近の発売予定を確認できる。
- 購入済み巻数・読了状態・予約中などの状態を管理できる。
- External Identity でログインしたアカウントに購読・購入情報を同期できる。
- ログインしない場合はデバイスのストレージで購読・購入情報を管理する（クラウド未保存）。
- カードに「タイトル / 巻数 / 著者 / 表紙サムネイル / 発売予定日」を表示する。
- 検索条件: タイトル / 著者 / 発売日 From / 出版社（部分一致・ひらがな正規化対応）。

### 1.1 ターゲット / ロケール

- ターゲットユーザー: 日本国内の漫画読者（個人）。
- ロケール: 日本語のみ提供。ただし i18n 抽出可能なリソース設計（`@angular/localize`）。

### 1.2 MVP スコープ

| 区分 | 機能 |
|---|---|
| In | 購読シリーズ登録・一覧 / 発売予定カレンダー & 直近一覧 / 購入済み巻数の管理 / 検索（タイトル・著者・発売日 From） / External Identity ログイン / 匿名利用（端末ローカル保存）|
| Out | 電子書籍ストア連携購入 / ソーシャル機能（フォロー・コメント） / AI レコメンデーション / スタンプラリー企画 |

> ※ 「latest」表記は **2026 年 4 月時点** の最新版を意味する。

---

## 2. アーキテクチャ概要 (How)

### 2.1 技術スタック

| 区分 | 採用技術 |
|---|---|
| Frontend | Angular **v21** + Tailwind CSS **v4** + tailwindcss-typography + Angular CDK / SSR (Hybrid Rendering) |
| Frontend Hosting | Azure Static Web Apps Standard（Managed Functions で SSR 実行）|
| Backend (API) | Azure Functions (.NET **10** Isolated Worker) — SWA 付属の Functions を使用 |
| Backend (Batch) | Azure Functions (.NET **10** Isolated Worker, **Consumption Plan**) + **Durable Functions** |
| Database | Azure SQL Database (Serverless / General Purpose, auto-pause 有効) — **Database First** |
| Storage / 表紙 | Azure Blob Storage（直接配信、CDN なし）|
| Identity | **Entra External ID (旧 AD B2C)**（Microsoft / Google / X(Twitter)）|
| Secrets | Key Vault + Managed Identity（App Settings に Key Vault 参照リンクで注入）|
| IaC | **Bicep**（modules: network / data / app / observability + main + env 別 param）|
| 監視 | Application Insights + Log Analytics + アラートルール |
| Feature Flag | Azure App Configuration |
| 開発環境 | DevContainer |
| テスト | Jest（フロント） / xUnit（バック） / Playwright + Testcontainers（E2E）|

### 2.2 コンポーネント構成図 (論理)

```
[Browser]
   │  HTTPS (SSR)
   ▼
[Static Web Apps]──[Managed Functions: Angular SSR]
   │ /.auth (Entra External ID)
   ▼
[SWA-linked Azure Functions API (.NET 10)]
   │       │
   │       ├── EF Core 10 ──► [Azure SQL Database]
   │       └── Blob SDK  ──► [Azure Blob Storage (cover images)]
   │
[Durable Functions Batch (Consumption)]
   │  Timer (毎日 03:00 JST)
   ├── Fetch (chaining: page-by-page) ─► 楽天 Books API
   └── Thumbnail (fan-out/in, 並列度 8) ─► Blob Storage

[Key Vault] ◄── Managed Identity ◄── Functions / SWA
[App Configuration] ── Feature Flag
[Application Insights / Log Analytics] ── 全コンポーネント
```

### 2.3 命名規則 / 環境

- 命名規則: Cloud Adoption Framework 推奨（`{prefix}-{env}-{region}-{resource}`）。
- 環境: **dev / prod** の 2 環境。PR ごとに **SWA Preview Environment** を自動生成。
- リージョン: **Japan East** のみ（DR 不要）。通貨 JPY、市場は日本国内。
- 可用性: SLA 目標は無指定（ベストエフォート）／ コスト最小優先。

---

## 3. ドメイン / データモデル

### 3.1 主要エンティティ

| テーブル | 概要 |
|---|---|
| `Users` | 内部 UserId(GUID)、表示名、IsDeleted、論理削除タイムスタンプ |
| `IdentityLinks` | (Provider, Subject(OID)) → 内部 UserId のマッピング |
| `Series` | 「シリーズ名 + 著者」で一意に集約したシリーズエンティティ |
| `Authors` / `SeriesAuthors` | 著者マスタ + 多対多関連 |
| `Publishers` | 出版社マスタ |
| `Volumes` | 巻：内部 GUID 主キー + ISBN-13 ユニーク列、巻数（手動補正可）、ReleaseDate (nullable)、ReleaseDateIsMonthOnly フラグ、CoverHash |
| `Subscriptions` | (UserId, SeriesId) ユニーク制約。論理削除 |
| `Purchases` | (UserId, VolumeId) と State（未購入 / 購入済 / 読了 / 予約中）。電子/紙の区別はしない |
| `ThumbnailAssets` | Blob 上のオブジェクトキー、サイズ、Hash |
| `BatchRuns` / `FailedItems` | バッチ実行履歴と失敗アイテム（DLQ 連携用）|

### 3.2 設計ポリシー

- 主キーは **GUID (uniqueidentifier) sequential**。
- テナンシーは **シングルテナント**。`UserId` 列で論理分離。
- 論理削除（`IsDeleted`）：購読 / 購入 / ユーザー。
- 監査: `CreatedAt` / `UpdatedAt` のみ保持（重量な監査ログは持たない）。
- スキーマ管理: **SSDT / DACPAC**（Source of Truth）+ GitHub Actions で migration デプロイ。
- 検索: SQL Database のフルテキスト検索 + 計算列（ひらがな正規化キー）で実現。LIKE は使わない。

### 3.3 シリーズ / 巻 集約ルール

- シリーズ集約キー: `(NormalizedSeriesName, PrimaryAuthor)`。
- 巻数は楽天 API のタイトルから正規表現抽出 → 管理画面 / ユーザー操作で手動修正可。
- 発売日が「月のみ」の場合は **その月の末日** を保存し `ReleaseDateIsMonthOnly=true` フラグ表示。「未定」は `null` を許容し UI で「未定」と表示。
- 重複: ISBN を主軸に **UPSERT**。表紙の hash で同一性を判定し再ダウンロードをスキップ。
- 1 ユーザー 1 シリーズ 1 購読（ユニーク制約）。

---

## 4. 楽天 Books API 連携 / バッチ

### 4.1 API

- エンドポイント: **BooksBookSearch / 20170404**。
- ジャンル: `booksGenreId = 001001`（コミック）に限定。
- 認証情報: `applicationId` を **App Settings → Key Vault 参照** で注入。Functions は Managed Identity で取得。
- レートリミット: **1 秒 1 リクエスト以下**（クライアント側にレートリミッターポリシー）。

### 4.2 バッチ仕様（Durable Functions）

- スケジュール: **毎日 03:00 JST フル収集**（Timer Trigger）。
- スキャン期間: **これから 6 ヶ月先まで**。**初回投入時は 6 ヶ月前も含む**。
- パターン: **Function chaining**（ページ収集を直列）→ **Fan-out / Fan-in**（サムネイル取得を並列）。
- サムネイル並列度: **8**（セマフォと反応を見て調整）。
- 失敗時: **リトライポリシー + DLQ (Storage Queue)** + Application Insights アラート。
- 手動起動: 管理者用 HTTP Trigger（Function Key + Entra 保護）を提供。

---

## 5. Frontend (Angular)

### 5.1 構成

- Angular **v21**, Standalone Components, Signals。
- 状態管理: **Signals のみ**（NgRx 未使用）。
- データ取得: HttpClient + interceptor（認証トークン付与）+ SSR で Transfer State。
- Tailwind v4 で **Design Token を一元管理**、ダークモード対応。
- Atomic Design: **Atoms / Molecules / Organisms / Templates / Pages** の 5 層。
- ディレクトリ:

```
src/app/
  atoms/
  molecules/
  organisms/
  templates/
  pages/
  core/      # SSR/auth/http interceptor 等
  shared/    # pipes, directives, design tokens
  features/  # 機能横断ロジック
```

### 5.2 UI / UX 仕様

- カードグリッド: **モバイル 2 列 / タブレット 4 列 / デスクトップ 6 列以上**（レスポンシブ）。
- カード表示要素: 表紙 / タイトル / 巻 / 著者 / 発売予定日 / 購読状態バッジ / 購入チェックボタン。
- 一覧: **無限スクロール（keyset pagination）**、デフォルトソートは **発売日昇順**。
- 「購読中のみ」トグルは **グローバル配置**。
- ビュー切替: **発売予定の週・月カレンダービュー** ⇄ **一覧ビュー**。
- 楽天 Books へのアフィリエイトリンクをカードに表示（ON/OFF 設定可）。
- 空状態: 説明文 + サンプルシリーズの提示。
- アクセシビリティ: **WCAG 2.1 AA 遵拠**（キーボード操作、ARIA、コントラスト）。
- PWA: 対応しない（MVP）。

### 5.3 ディスカバリ / 共有

- レコメンド機能 / シェア機能は **MVP では不要**（feature flag 下で将来検討）。

---

## 6. 認証 / セッション

- 実装: **Azure AD B2C / Entra External ID**（SWA Auth 統合）。
- IdP: **Microsoft / Google / X(Twitter)**。
- ユーザー識別: IdP 側 OID + 内部 UserId(GUID) マッピング。
- セッション: **SWA の Auth セッション**（`/.auth/me`）+ SSR で API トークンを Functions へ付与。
- 匿名 ⇄ ログイン: **初回ログイン時に「マージ / 上書き」をユーザーが選択**。
- ローカル保存技術: **IndexedDB（idb-keyval 等）**。
- デバイス間同期: **匿名でも QR コードで同期可能**（ログイン時はクラウド経由）。
- アカウント削除: **ソフト削除 → 期間後ハード削除**。
- ロール: `User` / `Admin`（シードデータ・運用用）。
- プライバシーポリシー / 利用規約: サイト内静的ページとして提供。

---

## 7. Backend API (Azure Functions)

- ランタイム: **.NET 10 Isolated Worker**。
- スタイル: **REST + OpenAPI 生成 (Swashbuckle)**。
- バージョニング: **URL パスバージョニング** (`/api/v1/...`)。
- レイヤリング: **Clean Architecture**（API / Application / Domain / Infrastructure）。
- 検証: **FluentValidation + DataAnnotations**。
- ロギング: `ILogger` 標準 → Application Insights。
- エラー応答: **RFC 7807 Problem Details**。
- セキュリティ: SWA Auth → Easy Auth でヘッダー付与 → Backend で署名 / クレーム検証。`Authorization=function` で SWA 経由限定アクセス。
- レート制御: **Functions 内ミドルウェアで Rate Limit**（API Management は導入しない）。

---

## 8. インフラ (Azure / Bicep)

- IaC: **Bicep modules（network / data / app / observability）+ main.bicep + env 別 param**。
- リソース命名: CAF 推奨（`{prefix}-{env}-{region}-{resource}`）。
- 環境: dev / prod。Auto-pause 有効な Serverless SQL でコスト抑制。
- バックアップ: Azure SQL の自動バックアップ標準（DR は不要）。
- シークレット: Key Vault + Managed Identity。App Settings に KV 参照を入れる。
- 配信: 表紙画像は Blob から直接配信（CDN を当面導入しない）。

---

## 9. 監視 / SRE

### 9.1 KPI / メトリクス

- DAU / WAU / 購読追加数 / リテンション。
- バッチ成功率 / 取得件数 / サムネイルキャッシュヒット率。
- API エラー率 / レスポンスタイム。

### 9.2 アラート

- バッチ失敗 → **Slack / Teams Webhook 通知**。
- API エラー率 > **1%** で発火。

### 9.3 性能目標

- フロント: **LCP < 2.5s / TTFB < 600ms**。
- API: **p95 < 500ms**。
- キャッシュ: API 応答に **ETag / If-None-Match** + SSR Transfer State + 静的アセットの CDN キャッシュ。

---

## 10. セキュリティ

- OWASP Top 10 対策:
  - **CSP / HSTS / Anti-CSRF トークン**。
  - **Output Encoding** で XSS 対策。
  - **EF Core** によるパラメタライズドクエリで SQL Injection 対策。
  - **Dependabot / CodeQL** による SCA。
- データ保持: 未ログイン匿名データはクラウド未保存。ログインユーザーデータは **退会まで永久保存**（ソフト削除後の保持期間を別途定義）。
- 監査ログ最小限・PII 最小化を原則とする。

---

## 11. テスト戦略

| 種類 | ツール | 範囲 |
|---|---|---|
| 単体（FE） | **Jest** | Angular コンポーネント / Service / Pipe / Directive |
| 単体（BE） | **xUnit** | Domain / Application 層 |
| 統合（BE） | **xUnit** | Functions エンドポイント / Durable orchestration（Testcontainers）|
| E2E | **Playwright** | 主要シナリオ：購読追加 / 検索 / 購入チェック / ログイン |
| 安定化 | **Testcontainers** | MSSQL コンテナ + Azurite (Blob/Queue) でシードデータ投入 |

- **ラインカバレッジ ≥ 80%** を PR ゲート。

### 11.1 E2E 設計原則（Page Object Model 必須）

- E2E は **Page Object Model (POM) を必ず採用**し、テストが UI 構造変更で大量に壊れない構造とする。
- ルール:
  - **`specs/` には DOM 操作・セレクタ・wait を書かない**。Page Object のメソッド呼び出しのみ。
  - **1 画面 = 1 Page クラス**（`*.page.ts`）。横断 UI（ヘッダ・カード・ダイアログ）は `components/` に独立 PO として配置。
  - **セレクタは必ず `data-testid` を使用**し、`selectors/` の定数として一元管理（テキスト・CSS クラスでの参照禁止）。
  - Page Object のメソッドは **ユーザー意図ベースの命名**（`addSubscription(seriesName)` 等）にし、内部の Locator を露出しない。
  - 共通振る舞い（ナビゲーション、認証セットアップ、a11y チェック、視覚的待機）は `base.page.ts` に集約。
  - Playwright の **Auto-waiting / `expect().toHaveX()`** を活用し、`waitForTimeout` 等の固定待機を禁止。
  - フロントの Atomic Design コンポーネントには **`data-testid` を埋め込む規約** をコンポーネント実装側にも展開。

---

## 12. CI / CD / 開発体験

### 12.1 パイプライン

- **GitHub Actions**: `lint → test → build → bicep what-if → deploy`。
- 認証は **OIDC + Federated Credential**（シークレットレス）。
- ブランチ戦略: **trunk-based**（main 一本 + 短寿命 feature branch）。

### 12.2 PR 品質ゲート

- ESLint / Prettier。
- `dotnet format` / Roslyn analyzers。
- Conventional Commits + PR title check。
- CodeQL / Dependabot。

### 12.3 リリース

- main → staging → prod のスロット運用。
- **Feature Flag (Azure App Configuration)** を併用：
  - External Identity ログイン段階ロールアウト。
  - レコメンド / ディスカバリ機能。
  - カレンダービュー A/B。

### 12.4 DevContainer

ベース: Microsoft `devcontainers/dotnet`。 features:

- .NET 10 SDK
- Node 22 LTS / pnpm
- **Angular CLI**
- Azure Functions Core Tools / Azurite
- SQL Server Tools (sqlcmd, sqlpackage)
- Bicep CLI
- **GitHub CLI / GitHub Copilot CLI**

---

## 13. OSS / 法務

- ライセンス: **MIT**。
- **SBOM** を CI で自動生成し、**OSS 情報ダイアログ** に下記を表示：
  - 使用 OSS パッケージ名 / バージョン / ライセンス
  - GitHub リポジトリへのリンク
- 楽天アフィリエイト規約に従い **「Powered by Rakuten Books」** クレジットを **フッタ + ダイアログ** に表示。
- プライバシーポリシー / 利用規約は静的ページとしてサイト内に同梱。

---

## 14. 留意事項（原文の継承 + 整理）

- フロントから直接呼ばれる WebAPI は SSR 経由に隠蔽し、**Functions は SWA 連携トークンが付いたリクエストのみ受け付ける**。
- OSS 情報・楽天クレジット・ライセンス確認用ダイアログを画面から開けるようにする。
- 漫画情報はカードグリッドで密度高く表示する。
- バッチは Durable Functions（**chaining → fan-out/in**）で長時間処理に耐える設計とする。
- Bicep で IaC を完結させる。Playwright の Flaky 対策に Testcontainers を活用。
- 開発は DevContainer 上で行う。

---

## 15. リポジトリディレクトリ構造

モノレポ構成。フロント・バックエンド・バッチ・IaC・E2E をひとつのリポジトリで管理する。

```
ComiCal/
├── .devcontainer/
│   ├── devcontainer.json
│   └── Dockerfile
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                 # lint / test / build
│   │   ├── cd-dev.yml             # bicep what-if → deploy (dev)
│   │   ├── cd-prod.yml            # bicep what-if → deploy (prod)
│   │   ├── pr-preview.yml         # SWA Preview Environment
│   │   └── codeql.yml
│   ├── dependabot.yml
│   └── CODEOWNERS
├── docs/
│   ├── init.md                    # 本ドキュメント
│   ├── adr/                       # Architecture Decision Records
│   ├── api/                       # OpenAPI 生成物 / 補足
│   └── diagrams/                  # 構成図 (drawio/mermaid)
├── infra/                         # Bicep IaC
│   ├── main.bicep
│   ├── modules/
│   │   ├── network.bicep
│   │   ├── data.bicep             # SQL / Storage
│   │   ├── app.bicep              # SWA / Functions / KV / AppConfig
│   │   └── observability.bicep    # App Insights / Log Analytics / Alerts
│   ├── params/
│   │   ├── dev.bicepparam
│   │   └── prod.bicepparam
│   └── README.md
├── src/
│   ├── frontend/                  # Angular v21 + SSR
│   │   ├── angular.json
│   │   ├── package.json
│   │   ├── tailwind.config.ts
│   │   ├── tsconfig.json
│   │   ├── public/
│   │   └── src/
│   │       ├── main.ts
│   │       ├── main.server.ts
│   │       ├── server.ts          # SSR entry (SWA Managed Functions)
│   │       ├── styles.css
│   │       ├── locale/            # @angular/localize messages
│   │       └── app/
│   │           ├── app.config.ts
│   │           ├── app.routes.ts
│   │           ├── atoms/
│   │           ├── molecules/
│   │           ├── organisms/
│   │           ├── templates/
│   │           ├── pages/
│   │           │   ├── home/
│   │           │   ├── calendar/
│   │           │   ├── search/
│   │           │   ├── subscriptions/
│   │           │   ├── settings/
│   │           │   └── legal/     # privacy / terms / oss
│   │           ├── core/          # auth, http interceptor, ssr, guards
│   │           ├── shared/        # pipes, directives, design tokens
│   │           └── features/      # 機能横断ロジック / signal stores
│   ├── backend/                   # .NET 10 ソリューション
│   │   ├── ComiCal.sln
│   │   ├── Directory.Build.props
│   │   ├── Directory.Packages.props
│   │   ├── api/                   # SWA-linked Functions API
│   │   │   └── ComiCal.Api/
│   │   │       ├── ComiCal.Api.csproj
│   │   │       ├── Program.cs
│   │   │       ├── host.json
│   │   │       ├── local.settings.json.sample
│   │   │       ├── Functions/     # HTTP triggers
│   │   │       ├── Middlewares/   # Auth / RateLimit / ProblemDetails
│   │   │       └── Models/        # Request/Response DTO
│   │   ├── batch/                 # Durable Functions バッチ
│   │   │   └── ComiCal.Batch/
│   │   │       ├── ComiCal.Batch.csproj
│   │   │       ├── Program.cs
│   │   │       ├── host.json
│   │   │       ├── Triggers/      # Timer / HTTP (manual)
│   │   │       ├── Orchestrators/ # Fetch chaining + Thumbnail fan-out
│   │   │       └── Activities/
│   │   ├── application/
│   │   │   └── ComiCal.Application/   # UseCases / Validators / Mappings
│   │   ├── domain/
│   │   │   └── ComiCal.Domain/        # Entities / ValueObjects / DomainServices
│   │   ├── infrastructure/
│   │   │   ├── ComiCal.Infrastructure/        # 共通: KV / AppConfig / Logging
│   │   │   ├── ComiCal.Infrastructure.Sql/    # EF Core 10 (DB First scaffold)
│   │   │   ├── ComiCal.Infrastructure.Blob/
│   │   │   └── ComiCal.Infrastructure.Rakuten/# 楽天 Books API クライアント + RateLimiter
│   │   └── shared/
│   │       └── ComiCal.Shared/        # 共有 DTO / Result / Errors
│   ├── db/                        # Database First (SSDT/DACPAC)
│   │   ├── ComiCal.Database.sqlproj
│   │   ├── Schemas/
│   │   │   └── dbo/
│   │   │       ├── Tables/
│   │   │       │   ├── Users.sql
│   │   │       │   ├── IdentityLinks.sql
│   │   │       │   ├── Series.sql
│   │   │       │   ├── Authors.sql
│   │   │       │   ├── SeriesAuthors.sql
│   │   │       │   ├── Publishers.sql
│   │   │       │   ├── Volumes.sql
│   │   │       │   ├── Subscriptions.sql
│   │   │       │   ├── Purchases.sql
│   │   │       │   ├── ThumbnailAssets.sql
│   │   │       │   ├── BatchRuns.sql
│   │   │       │   └── FailedItems.sql
│   │   │       ├── Views/
│   │   │       ├── Indexes/
│   │   │       └── FullText/
│   │   ├── Scripts/
│   │   │   ├── PreDeploy/
│   │   │   ├── PostDeploy/
│   │   │   └── Seed/
│   │   └── publish-profiles/
│   │       ├── dev.publish.xml
│   │       └── prod.publish.xml
│   └── tests/
│       ├── backend/                       # .NET テスト
│       │   ├── ComiCal.Domain.Tests/
│       │   ├── ComiCal.Application.Tests/
│       │   ├── ComiCal.Api.Tests/         # 統合 (WebApplicationFactory)
│       │   ├── ComiCal.Batch.Tests/       # Durable + Testcontainers
│       │   └── ComiCal.Infrastructure.Tests/
│       └── e2e/                           # Playwright (Page Object Model)
│           ├── package.json
│           ├── playwright.config.ts
│           ├── fixtures/                  # Testcontainers (MSSQL / Azurite) セットアップ
│           ├── pages/                     # Page Object: 1 画面 = 1 クラス
│           │   ├── base.page.ts           # 共通基底 (navigation, waits, a11y)
│           │   ├── home.page.ts
│           │   ├── calendar.page.ts
│           │   ├── search.page.ts
│           │   ├── subscriptions.page.ts
│           │   ├── settings.page.ts
│           │   └── login.page.ts
│           ├── components/                # 横断コンポーネント PO (Header, Card, Dialog)
│           ├── specs/                     # POを呼ぶだけの薄いテスト
│           │   ├── auth.spec.ts
│           │   ├── subscribe.spec.ts
│           │   ├── search.spec.ts
│           │   └── purchase.spec.ts
│           ├── selectors/                 # data-testid 定数 (UI とテストの結合点)
│           └── seeds/
├── tools/
│   ├── scripts/                   # 開発補助スクリプト (pnpm/dotnet/bicep)
│   ├── sbom/                      # SBOM 生成設定
│   └── oss-report/                # OSS ライセンス情報生成
├── staticwebapp.config.json       # SWA ルーティング / 認証 / ヘッダ (CSP/HSTS)
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .nvmrc
├── global.json                    # .NET SDK ピン留め
├── pnpm-workspace.yaml
├── package.json                   # ルートスクリプト
├── LICENSE                        # MIT
├── README.md
└── SECURITY.md
```

### 15.1 構成方針

- **モノレポ**: フロント (`src/frontend`)、バック (`src/backend`)、DB (`src/db`)、テスト (`src/tests`)、IaC (`infra`) を 1 リポジトリで管理し、PR 単位で全レイヤーを跨いだ変更を追跡可能にする。
- **`src/` 配下に実装資産を集約**: アプリケーションコード（frontend/backend）・スキーマ（db）・テスト（tests/{backend,e2e}）を `src/` に内包し、IaC・ドキュメント・ツール類とは明確に分離する。
- **バックエンドは Clean Architecture** をプロジェクト分割で物理的に強制（`domain` ← `application` ← `api`/`batch`、`infrastructure.*` は依存注入で接続）。
- **DB は SSDT/DACPAC を SoT** とし、EF Core はそこから scaffold する（Database First）。
- **Bicep modules** は責務単位（network/data/app/observability）で分割、`params/{env}.bicepparam` で環境差分を管理。
- **Playwright (`src/tests/e2e`) は独立ワークスペース** とし、Testcontainers でテストデータ環境を都度構築。
- ルートに **`staticwebapp.config.json`** を配置し SWA のルーティング・認証ジャーニ・CSP/HSTS ヘッダを宣言的に管理。

---

## 16. 将来拡張候補（非 MVP）

- Web Push / メール通知（**ユーザー設定可能なリマインドタイミング**, 例: 7 日前 / 前日 / 当日）。
- 未読・既読管理 / ウィッシュリスト。
- Azure AI Search でフルテキスト検索強化。
- ディスカバリ / レコメンド。
- 共有 OG カード / 公開リンク。
- Azure Front Door (CDN) 経由配信。
