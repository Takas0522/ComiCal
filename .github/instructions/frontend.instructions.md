---
description: 'Use when implementing Angular v21 standalone components, Signals (input/output/computed/effect), Tailwind CSS v4 (@theme tokens), Zoneless change detection, SSR/TransferState, Atomic Design layout, or WCAG 2.1 AA accessibility under src/frontend/.'
applyTo: 'src/frontend/**'
---

# Frontend (Angular v21) Instructions

## Architecture

- **Standalone Components 必須**（`standalone: true`、NgModule 新規作成禁止）
- **Signals ベース状態管理**（NgRx 不使用）
- **Zoneless Change Detection**（Zone.js 依存禁止、`NgZone.run()` 等の手動呼び出し禁止）
- **SSR (Hybrid Rendering)**：TransferState で二重 HTTP を防ぐ
- **Atomic Design 5 層**: `atoms / molecules / organisms / templates / pages`
- ディレクトリ:
  - `core/` — SSR / auth / http interceptor / guards
  - `shared/` — pipes / directives / design tokens
  - `features/` — 機能横断ロジック / signal stores

## Signals 利用ルール

- 入力: `input()` / `input.required<T>()`（`@Input` デコレータより優先）
- 出力: `output<T>()`（`@Output` より優先）
- 内部状態: `signal<T>()`
- 派生値: `computed<T>()`
- 副作用: `effect()`（ロジック実行場所として濫用しない、副作用専用）
- 非同期フローは引き続き RxJS（debounce / switchMap 等）

## Tailwind CSS v4

- **Design Token は `@theme` ディレクティブで一元管理**（`tailwind.config.js` は使わない）
- ダークモードは `dark:` バリアント
- 任意値（`h-[100px]` 等）は最小限。token を優先
- **クラス名のテンプレートリテラル組み立て禁止**（Tailwind が静的解析できなくなる）

## SSR / Transfer State

- サーバー専用 API（`localStorage`, `window` 等）はガード（`isPlatformBrowser()`）
- SSR 中の HTTP レスポンスは `TransferState` でクライアントに転送し二重 HTTP を防ぐ
- 非決定的コンテンツ（タイムスタンプ・乱数）はサーバー時に避ける（hydration mismatch を防ぐ）

## アクセシビリティ

- **WCAG 2.1 AA 遵拠**（キーボード操作・ARIA・コントラスト比）
- Angular v21 の組み込み ARIA ディレクティブを優先利用

## i18n

- 日本語のみ提供だが、すべての UI 文字列は `@angular/localize` で抽出可能に
- 文字列はテンプレートにハードコードせず、`i18n` 属性 / `$localize` を使用
- ロケールリソースは `src/locale/` に配置

## テストデータ属性

- すべての **インタラクティブ要素に `data-testid`** を付与（E2E から参照）
- `data-testid` は **kebab-case**、画面責務 + 役割を含む（例: `subscribe-button`、`search-input`）

## UI / UX 仕様

- カードグリッド: モバイル 2 列 / タブレット 4 列 / デスクトップ 6 列以上
- 一覧: 無限スクロール（**keyset pagination**）、デフォルトソートは発売日昇順
- ビュー切替: 週・月カレンダービュー ⇄ 一覧ビュー
- 楽天 Books へのアフィリエイトリンクは ON/OFF 設定可

## ローカル保存（匿名利用）

- **IndexedDB**（idb-keyval 等）を使用
- `localStorage` は使わない
- 匿名 ⇄ ログイン時は「マージ / 上書き」をユーザーに選択させる

## アンチパターン

- ❌ NgModule の新規作成
- ❌ `standalone: false`
- ❌ BehaviorSubject / Subject による状態管理（Signals を使う）
- ❌ `NgZone.run()` / `ChangeDetectorRef.detectChanges()` の手動呼び出し
- ❌ `data-testid` なしの interactive 要素
- ❌ クラス名の動的構築（`'bg-' + color`）
- ❌ サーバーサイドで `window` / `document` / `localStorage` 直接アクセス
