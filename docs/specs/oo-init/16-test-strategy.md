# 16. テスト戦略

## 16.1 テストピラミッド

| 層 | ツール | 範囲 |
|---|---|---|
| Unit (FE) | **Jest** | Angular Component / Service / Pipe / Directive / Signal store |
| Unit (BE) | **xUnit v3** | Domain / Application 層 |
| Integration (BE) | **xUnit v3 + Testcontainers** (MSSQL / Azurite) + WebApplicationFactory | Functions エンドポイント、Durable orchestration |
| E2E | **Playwright** | 主要シナリオ（購読追加 / 検索 / 購入チェック / ログイン）|

> Angular v21 の既定テストランナーは Vitest だが、本プロジェクトは **Jest を継続採用**。

## 16.2 カバレッジゲート

- **ラインカバレッジ ≥ 80%** を **PR ゲート**（CI で失敗）。
- 対象: `src/frontend/**`, `src/backend/api/**`, `src/backend/application/**`, `src/backend/domain/**`, `src/backend/batch/**`。
- 対象外: 自動生成コード、`Migrations/`、`*.Designer.cs`。

## 16.3 フロント単体テスト規約

- TestBed + `provideHttpClient(withFetch())` + `provideHttpClientTesting()` のシンプルな構成。
- Signals テストは `effect` + `flush` パターン、または `runInInjectionContext` を使用。
- `fakeAsync` / `tick` を活用し、`setTimeout` の生使いを禁止。
- セレクタは `data-testid` で。CSS クラス名や日本語テキストでの DOM 取得は禁止。

## 16.4 バックエンド単体テスト規約

- **Domain / Application 層は外部依存ゼロ**（純粋関数 / モック不要を志向）。
- Repository は **インターフェースをモック**（NSubstitute）。EF Core 実物は Integration で。
- AAA パターン（Arrange / Act / Assert）の明示。

## 16.5 統合テスト（Testcontainers）

- **MSSQL コンテナ + Azurite (Blob / Queue)** をテストごとに起動。
- スキーマ適用は **DACPAC publish** をフィクスチャで実行（実プロダクションと同じスキーマ）。
- シードデータは `IAsyncLifetime` の `InitializeAsync` で投入、テスト終了時にコンテナ破棄。
- WebApplicationFactory で Functions Worker を in-process ホスト化し、HTTP エンドポイントを叩く。

## 16.6 Durable Functions テスト

- Orchestrator は **`DurableTaskClient` フェイク**で Activity をモック化し、決定論性を検証。
- Activity 単体は通常の xUnit + Testcontainers で I/O ごと検証。
- `ContinueAsNew` の境界、`RetryOptions` の動作を必ずテスト。

## 16.7 E2E (Playwright) ルール — Page Object Model 必須

### 必須ルール

1. **`specs/` には DOM 操作・セレクタ・wait を書かない**。Page Object のメソッド呼び出しのみ。
2. **1 画面 = 1 Page クラス** (`*.page.ts`)。横断 UI（ヘッダ・カード・ダイアログ）は `components/` に独立 PO。
3. **セレクタは `data-testid`** を必須使用し、`selectors/` に定数として一元管理。テキスト / CSS クラスでの参照禁止。
4. Page Object のメソッドは **ユーザー意図ベースの命名**（`addSubscription(seriesName)` 等）。Locator を露出しない。
5. 共通振る舞い（navigation / auth / a11y / 視覚的待機）は `base.page.ts` に集約。
6. **Auto-waiting / `expect().toHaveX()`** を活用し、`waitForTimeout` 等の固定待機を **禁止**。
7. フロントの Atomic Design コンポーネントには **`data-testid` 埋め込みを必須化**。

### ディレクトリ

```
src/tests/e2e/
├── playwright.config.ts
├── fixtures/      # Testcontainers (MSSQL / Azurite) セットアップ
├── pages/         # base.page.ts + {home,calendar,search,subscriptions,settings,login}.page.ts
├── components/    # Header / Card / Dialog の横断 PO
├── specs/         # auth / subscribe / search / purchase
├── selectors/     # data-testid 定数
└── seeds/         # シードデータ
```

### a11y チェック

- 主要な spec で **axe-core** を 1 回以上実行（`@axe-core/playwright`）。
- WCAG 2.1 AA 違反を CI 失敗にする。

## 16.8 テストデータ

- **すべてのテストはシードを自前で投入**し、他テストに依存しない（並列実行可能）。
- バッチ統合テストは楽天 API を直接呼ばず、**WireMock.Net** で固定レスポンスを返す。

## 16.9 Flaky 対策

- Playwright は Testcontainers で SQL/Blob を再現可能に。
- `retries=2`（CI のみ）+ Flaky の自動 issue 化（GitHub Actions）。
- `waitForLoadState('networkidle')` は使わず、明示的な `expect().toBeVisible()` を使う。

## 16.10 走らせ方

```bash
pnpm --filter frontend test           # Jest
dotnet test src/backend/ComiCal.sln   # xUnit + Testcontainers
pnpm --filter e2e test                # Playwright
```
