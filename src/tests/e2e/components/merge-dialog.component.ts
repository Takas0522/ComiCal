import { Locator, Page, expect } from '@playwright/test';
import { MERGE } from '../selectors/merge.selectors';

/**
 * Auto-prompted dialog shown to a freshly authenticated user when local
 * IndexedDB contains uncommitted subscriptions/purchases.
 *
 * Selectors map to `MergePromptDialogComponent` (organism). The dialog is
 * an `<dialog role="dialog">`, so all assertions use auto-waiting locators.
 */
export class MergeDialog {
  private readonly root: Locator;
  private readonly subCount: Locator;
  private readonly purchaseCount: Locator;
  private readonly accept: Locator;
  private readonly discard: Locator;
  private readonly snooze: Locator;

  constructor(private readonly page: Page) {
    this.root = page.getByTestId(MERGE.dialog);
    this.subCount = page.getByTestId(MERGE.subCount);
    this.purchaseCount = page.getByTestId(MERGE.purchaseCount);
    this.accept = page.getByTestId(MERGE.accept);
    this.discard = page.getByTestId(MERGE.discard);
    this.snooze = page.getByTestId(MERGE.snooze);
  }

  async expectVisible(): Promise<void> {
    await expect(this.root).toBeVisible();
  }

  async expectClosed(): Promise<void> {
    await expect(this.root).toBeHidden();
  }

  async expectCounts(subscriptions: number, purchases: number): Promise<void> {
    await expect(this.subCount).toHaveText(String(subscriptions));
    await expect(this.purchaseCount).toHaveText(String(purchases));
  }

  async accepting(): Promise<void> {
    await this.accept.click();
  }

  async discarding(): Promise<void> {
    await this.discard.click();
  }

  async snoozing(): Promise<void> {
    await this.snooze.click();
  }
}
