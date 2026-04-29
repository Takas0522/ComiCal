---
description: 'Use when writing Playwright E2E tests with Page Object Model, data-testid selectors, axe a11y checks, or Testcontainers fixtures under src/tests/e2e/.'
applyTo: 'src/tests/e2e/**'
---

# E2E (Playwright Page Object Model) Instructions

## 全体構成

```
src/tests/e2e/
├── playwright.config.ts
├── fixtures/                # Testcontainers (MSSQL/Azurite) セットアップ
├── pages/                   # 1 画面 = 1 Page クラス
│   ├── base.page.ts         # 共通基底（navigation, waits, a11y）
│   ├── home.page.ts
│   ├── calendar.page.ts
│   └── ...
├── components/              # 横断 PO（Header, Card, Dialog）
├── specs/                   # PO を呼ぶだけの薄いテスト
├── selectors/               # data-testid 定数
└── seeds/
```

## Page Object Model: 厳格ルール

### specs/ には書かない

- ❌ DOM 操作（`page.click(...)`、`page.fill(...)`）
- ❌ セレクタ（CSS、XPath、テキスト）
- ❌ wait（`waitForSelector`、`waitForTimeout`）
- ✅ Page Object のメソッド呼び出しのみ

### 1 画面 = 1 Page クラス

- 配置: `pages/<screen>.page.ts`
- `base.page.ts` を継承
- 横断 UI（ヘッダ・カード・ダイアログ）は `components/` に独立 PO として配置

### メソッド命名

- **ユーザー意図ベース**: `addSubscription(seriesName)`、`switchToCalendarView()`
- 内部 Locator は **private**、外部に露出しない

```typescript
import { Page, Locator } from '@playwright/test';
import { SUBSCRIPTIONS } from '../selectors/subscriptions.selectors';

export class SubscriptionsPage extends BasePage {
  private readonly subscribeButton: Locator;
  private readonly seriesInput: Locator;

  constructor(page: Page) {
    super(page);
    this.subscribeButton = page.getByTestId(SUBSCRIPTIONS.subscribeButton);
    this.seriesInput = page.getByTestId(SUBSCRIPTIONS.seriesInput);
  }

  async addSubscription(seriesName: string): Promise<void> {
    await this.seriesInput.fill(seriesName);
    await this.subscribeButton.click();
  }
}
```

## セレクタ戦略

- **必ず `data-testid`** を使用（CSS クラス・テキストでの参照禁止）
- `selectors/` 配下に **定数として一元管理**
- フロントの Atomic Design コンポーネントには `data-testid` を埋め込む規約をコンポーネント側にも展開

```typescript
// selectors/subscriptions.selectors.ts
export const SUBSCRIPTIONS = {
  subscribeButton: 'subscribe-button',
  seriesInput: 'series-name-input',
} as const;
```

## 待機戦略

- **Auto-waiting / `expect().toHaveX()`** を活用
- **`waitForTimeout` 禁止**（固定待機禁止）
- 共通の待機ロジックは `base.page.ts` に集約

## fixture / セットアップ

- 認証セットアップ・テストデータ投入は **`fixtures/`** に集約
- **Testcontainers** で MSSQL + Azurite を都度構築（テストデータの分離）

## アクセシビリティ

- 主要シナリオには `@axe-core/playwright` で a11y チェックを組み込む
- WCAG 2.1 AA 違反を fail とする

## CI 統合

- 並列ワーカー実行で速度確保
- 失敗時 Trace Viewer / スクリーンショットを成果物として保存

## 主要シナリオ（init.md §11）

- 購読追加 (`subscribe.spec.ts`)
- 検索 (`search.spec.ts`)
- 購入チェック (`purchase.spec.ts`)
- ログイン (`auth.spec.ts`)

## アンチパターン

- ❌ spec 内に CSS セレクタ / XPath / テキスト一致
- ❌ `await page.waitForTimeout(...)` 使用
- ❌ Page Object の private locator を spec から触る
- ❌ data-testid 文字列をハードコード（必ず `selectors/` 定数から）
- ❌ 共通フローを spec に直書き（base.page.ts / fixtures に集約）
- ❌ Page Object のメソッド名が技術ベース（`clickSubmitButton` ではなく `submit`）
