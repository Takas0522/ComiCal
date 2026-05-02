# 10. フロントエンド仕様 (Angular v21)

## 10.1 構成

- **Angular v21 Standalone Components** + **Signals (input/output/computed/effect)**。
- **Zoneless 変更検知**（`provideExperimentalZonelessChangeDetection()` / latest 安定版 API）。
- **NgModule は使わない**。`app.config.ts` / `app.routes.ts` で構成。
- 状態管理: **Signals のみ**（NgRx は採用しない）。
- データ取得: `HttpClient` + interceptor（認証ヘッダ / エラー / トランスフォーム）+ **SSR Transfer State**。
- スタイリング: **Tailwind CSS v4** を [Angular 公式ガイド](https://angular.jp/guide/tailwind) に従って導入（`ng add tailwindcss`、または手動セットアップで `tailwindcss` + `@tailwindcss/postcss` + `postcss` をインストールし、`.postcssrc.json` に `@tailwindcss/postcss` プラグインを設定、`src/styles.css` に `@import 'tailwindcss';` を記述）。Design Token は `@theme` ディレクティブで一元管理し、ダークモード対応。`tailwind.config.ts` は作成しない。
- フォント / アイコン: ローカル同梱（外部 CDN を使わない、CSP 簡素化）。

## 10.2 SSR (Hybrid Rendering)

- Angular **Hybrid Rendering**（route ごとに Static / SSR / CSR を選択）。
- ホスト: SWA Standard の **Managed Functions**（`server.ts`）。
- 認証必須ページは SSR、法務ページは Static、設定ページは CSR-only。
- **Transfer State** を使い、ハイドレーション時に再 fetch しない。

## 10.3 Atomic Design

```
src/frontend/src/app/
├── atoms/        # Button, Badge, Icon, Skeleton 等
├── molecules/    # Card, FormField, Toggle 等
├── organisms/    # Header, BottomNav, CardGrid, Calendar 等
├── templates/    # PageLayout, AuthLayout, LegalLayout
├── pages/        # home, calendar, search, subscriptions, settings, login, legal
├── core/         # auth / http interceptor / ssr / guards / error handler
├── shared/       # pipes, directives, design tokens
└── features/     # 機能横断 signal stores / facades
```

## 10.4 ルーティング

| Path             | Component         | Render | Auth                       |
| ---------------- | ----------------- | ------ | -------------------------- |
| `/`              | HomePage          | SSR    | optional                   |
| `/calendar`      | CalendarPage      | SSR    | optional                   |
| `/search`        | SearchPage        | SSR    | optional                   |
| `/subscriptions` | SubscriptionsPage | SSR    | required（匿名はローカル） |
| `/series/:id`    | SeriesDetailPage  | SSR    | optional                   |
| `/settings`      | SettingsPage      | CSR    | optional                   |
| `/login`         | LoginPage         | SSR    | unauthenticated only       |
| `/legal/privacy` | PrivacyPage       | Static | -                          |
| `/legal/terms`   | TermsPage         | Static | -                          |
| `/legal/oss`     | OssPage           | Static | -                          |

## 10.5 Signals 設計指針

- コンポーネント間データ受け渡しは `input.required<T>()` / `output<T>()`。
- 派生値は `computed()`。副作用は `effect()`（SSR では `afterNextRender` で抑制）。
- グローバル状態は `features/` 配下の **signal store**（Service + signal）として実装。NgRx は使わない。

## 10.6 HTTP / API クライアント

- `provideHttpClient(withFetch(), withInterceptors([...]))`。
- インターセプター:
  - `authInterceptor`: SWA `/.auth` のセッショントークンを Functions API に転送。
  - `errorInterceptor`: RFC 7807 を `ProblemDetails` 型に正規化、トースト表示用イベントを emit。
  - `transferStateInterceptor`: SSR で取得したレスポンスを Transfer State に積み、ブラウザ側は読み出すだけにする。
- API クライアントは **OpenAPI から自動生成**（`update-openapi` Skill 経由で同期）。

## 10.7 データテストID 規約

- すべての操作可能要素に **`data-testid="<atomic>-<intent>"`** を付与必須。
  - 例: `card-volume`, `btn-subscribe`, `toggle-subscribed-only`。
- セレクタは `src/tests/e2e/selectors/` に定数として一元管理し、E2E テストはそれのみを参照。

## 10.8 i18n

- `@angular/localize` でメッセージ抽出可能な構造に統一（`i18n` 属性 / `$localize`）。
- MVP は **日本語 (ja-JP) のみ** 出荷。`xliff` ファイルは生成のみ行いリポジトリに保管。

## 10.9 ダークモード

- Tailwind v4 `@theme` で `--color-*` トークンを Light / Dark で切替。
- 設定: `Light / Dark / System`。System は `prefers-color-scheme` 追従。
- ハイドレーション直後のフラッシュ防止に SSR で `class="dark"` を出力。

## 10.10 PWA

- **MVP では PWA 対応しない**（Service Worker / Manifest を組み込まない）。
- Web Push を実装する将来フェーズで再検討。

## 10.11 パフォーマンス目標

| 指標                  | 目標                             |
| --------------------- | -------------------------------- |
| LCP                   | < 2.5s                           |
| TTFB                  | < 600ms                          |
| CLS                   | < 0.1                            |
| 初期 JS（gzip）       | < 200KB                          |
| 表紙画像 LCP リソース | preload + `fetchpriority="high"` |

## 10.12 アクセシビリティ規約

- 各コンポーネントに `aria-*`、キーボード操作、フォーカスリングを実装。
- E2E に **axe-core** 実行を必須シナリオに 1 回以上組み込む。
