---
name: add-e2e-spec
description: 'Use when adding a new Playwright E2E scenario or a new Page Object under src/tests/e2e/. Enforces Page Object Model (specs only call POM methods), data-testid selectors centralized in selectors/, ban on waitForTimeout (auto-waiting / expect().toHaveX()), and intent-based method naming.'
argument-hint: '<screenName> <scenarioName>'
allowed-tools: Read, Write, Edit, Bash
---

# add-e2e-spec

## 配置

- Page Object: `src/tests/e2e/pages/<screen>.page.ts`
- Component PO: `src/tests/e2e/components/<comp>.component.ts`
- セレクタ定数: `src/tests/e2e/selectors/<screen>.selectors.ts`
- spec: `src/tests/e2e/specs/<scenario>.spec.ts`

## 必須要件

1. **specs は POM メソッドのみ呼ぶ**
   - DOM 操作・セレクタ・wait は spec に書かない
2. **セレクタは `data-testid` のみ**（CSS / text マッチ禁止）
3. **`waitForTimeout` 禁止**（auto-waiting / `expect().toHaveX()` を使う）
4. **1 画面 = 1 Page クラス**（`base.page.ts` を継承）
5. **メソッド名はユーザー意図ベース**（`addSubscription`、`searchByTitle`）
6. **selectors/ に定数で一元管理**

## Page Object テンプレート

```typescript
import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { SUBSCRIPTIONS } from '../selectors/subscriptions.selectors';

export class SubscriptionsPage extends BasePage {
  private readonly seriesInput: Locator;
  private readonly subscribeButton: Locator;
  private readonly listItems: Locator;

  constructor(page: Page) {
    super(page);
    this.seriesInput = page.getByTestId(SUBSCRIPTIONS.seriesInput);
    this.subscribeButton = page.getByTestId(SUBSCRIPTIONS.subscribeButton);
    this.listItems = page.getByTestId(SUBSCRIPTIONS.listItem);
  }

  async goto(): Promise<void> {
    await this.page.goto('/subscriptions');
    await this.subscribeButton.waitFor({ state: 'visible' });
  }

  async addSubscription(seriesName: string): Promise<void> {
    await this.seriesInput.fill(seriesName);
    await this.subscribeButton.click();
  }

  async itemCount(): Promise<number> {
    return await this.listItems.count();
  }
}
```

## spec テンプレート

```typescript
import { expect, test } from '@playwright/test';
import { SubscriptionsPage } from '../pages/subscriptions.page';

test('ユーザーが購読を追加できる', async ({ page }) => {
  const subs = new SubscriptionsPage(page);
  await subs.goto();
  await subs.addSubscription('SPY×FAMILY');
  expect(await subs.itemCount()).toBe(1);
});
```

## チェックリスト

- [ ] spec が POM メソッド以外を呼んでいない
- [ ] data-testid 文字列が `selectors/` 定数化
- [ ] `waitForTimeout` を使っていない
- [ ] axe a11y チェックを主要シナリオに追加（必要時）

## 関連

- `.github/instructions/e2e.instructions.md`
- テンプレート: `templates/page.template.ts`, `templates/spec.template.ts`

## アンチパターン

- ❌ spec で `page.click('css.foo')`
- ❌ data-testid 文字列のハードコード
- ❌ `await page.waitForTimeout(500)`
- ❌ private locator を spec から触る
