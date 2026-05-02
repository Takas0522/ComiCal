import { expect } from '@playwright/test';
import { test } from '../fixtures/app.fixture';

test.describe('ホームページ', () => {
  test('ホームページが表示される', async ({ homePage }) => {
    await homePage.goto();
    await homePage.isHeroVisible();
  });

  test('シリーズ一覧が表示される', async ({ homePage }) => {
    await homePage.goto();
    await homePage.isHeaderVisible();
    await homePage.isSeriesListVisible();
  });
});
