/**
 * Search smoke spec.
 *
 * SKIP RATIONALE: see specs/home.spec.ts. All cases are wrapped in
 * `test.skip(true, ...)` until the Phase 1 stack is wired into CI.
 */
import { test } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';

test.describe('search', () => {
  test.skip(true, SKIP_REASON);

  test('header search submits to /search?q=...&tab=series and lists series', async ({
    homePage,
    header,
    searchPage,
  }) => {
    await homePage.goto();
    await header.submitSearch('ワンピース');
    await searchPage.expectUrlHasQuery('ワンピース', 'series');
    await searchPage.expectTabListVisible();
    await searchPage.expectResultsForTab('series');
  });

  test('switching to volumes tab shows volume cards', async ({ searchPage }) => {
    await searchPage.gotoWith('ワンピース', 'series');
    await searchPage.selectTab('volumes');
    await searchPage.expectResultsForTab('volumes');
  });

  test('load-more grows the result list', async ({ searchPage }) => {
    await searchPage.gotoWith('ワンピース', 'volumes');
    await searchPage.expectResultsForTab('volumes');
    await searchPage.loadMore('volumes');
  });
});
