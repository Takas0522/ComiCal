import { test } from '../fixtures/app.fixture';

test.describe('検索', () => {
  test('キーワードで検索できる', async ({ searchPage }) => {
    await searchPage.goto();
    await searchPage.isPageVisible();
    await searchPage.searchFor('SPY×FAMILY');
    await searchPage.isEmptyResultsShown();
  });

  test('検索結果なしの場合メッセージが表示される', async ({ searchPage }) => {
    await searchPage.goto();
    await searchPage.isPageVisible();
    await searchPage.searchFor('xyzzy_このキーワードには結果がありません_12345');
    await searchPage.isEmptyResultsShown();
  });
});
