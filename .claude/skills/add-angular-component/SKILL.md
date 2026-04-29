---
name: add-angular-component
description: 'Use when adding a new Angular v21 standalone UI component (atoms / molecules / organisms / templates) under src/frontend/. Generates Atomic Design layout, Signals-based state (input/output/computed), Tailwind v4 utilities, OnPush change detection, data-testid attributes, and Jest spec scaffold.'
argument-hint: '<atomicLevel: atoms|molecules|organisms|templates> <componentName>'
allowed-tools: Read, Write, Edit, Bash
---

# add-angular-component

## 適用前提

- 配置: `src/frontend/src/components/<atomicLevel>/<component-name>/`
- 言語/FW: Angular v21、TypeScript strict、Tailwind v4
- テスト: Jest + Testing Library

## 手順

1. **配置先決定**
   - atoms: ボタン・入力など最小単位
   - molecules: atoms 結合（FormField 等）
   - organisms: 機能ユニット（Header、SubscriptionCard 等）
   - templates: ページレイアウト

2. **生成するファイル**
   - `<name>.component.ts`（standalone、`ChangeDetectionStrategy.OnPush`）
   - `<name>.component.html`
   - `<name>.component.spec.ts`
   - `index.ts`（barrel）

3. **コーディング規約**（必ず適用）
   - `standalone: true`
   - `imports: [...]` で必要なものだけ取り込み
   - state は **Signals** (`signal()` / `computed()` / `effect()`) を使う
   - 入出力は `input()` / `output()` 関数 API（`@Input()` / `@Output()` デコレータ廃止）
   - 制御フローは `@if` / `@for` / `@switch`（`*ngIf` / `*ngFor` 廃止）
   - 子テンプレート参照は `viewChild()` 関数
   - スタイル: Tailwind v4 ユーティリティクラスのみ（`@layer components` で再利用部品化）
   - **テスト用 `data-testid` 属性を主要 DOM に必ず付与**（kebab-case）
   - DI: `inject()` 関数を優先（コンストラクタ DI ではなく）

4. **テストの最低要件**
   - 描画スモークテスト（`render` でレンダリング確認）
   - input/output の挙動 1 件
   - data-testid の存在検証

5. **barrel export**
   - 同階層に `index.ts` がない場合は作成し `export *` を追加

## 参照

- テンプレート: `templates/component.template.ts`
- 例: `examples/card.example.ts`
- 詳細: [Angular v21 Components](https://angular.dev/guide/components)

## アンチパターン

- ❌ NgModule の使用
- ❌ `*ngIf` / `*ngFor` / `*ngSwitch`
- ❌ `@Input()` / `@Output()` デコレータ
- ❌ ChangeDetectionStrategy 未指定
- ❌ data-testid なしの主要要素
