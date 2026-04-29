import { expect, test } from '@playwright/test';
import { {{Screen}}Page } from '../pages/{{screen}}.page';

test.describe('{{Screen}}', () => {
  test('{{scenario description}}', async ({ page }) => {
    const target = new {{Screen}}Page(page);
    await target.goto();

    // TODO: PO メソッドのみ呼び、expect は state 確認に絞る
  });
});
