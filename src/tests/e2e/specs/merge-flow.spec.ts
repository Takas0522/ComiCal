/**
 * Anonymous → authenticated merge flow.
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 *
 * Validates the spec at docs/specs/oo-init/13-ux-a11y-i18n.md. The merge
 * service stages local IndexedDB items and POSTs them to /api/me/* once
 * the user accepts; "discard" wipes IndexedDB without an API call;
 * "later" leaves IndexedDB intact and surfaces the manual-trigger row in
 * /settings.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';

const SEED_QUERY = 'ワンピース';
const SEED_ISBN = '9784088838212';

test.describe('merge flow — accept', () => {
  test.skip(true, SKIP_REASON);

  test('local 2 subs + 1 purchase → login → accept → 3件 引き継ぎ', async ({
    homePage,
    header,
    searchPage,
    volumeByIsbnPage,
    loginPage,
    mergeDialog,
    toast,
  }) => {
    // (a) Anonymous: add 2 series subscriptions
    await homePage.goto();
    await header.expectAnonymous();
    await searchPage.gotoWith(SEED_QUERY, 'series');
    await searchPage.addSeriesToSubscriptions(0);
    await searchPage.addSeriesToSubscriptions(1);
    await header.expectLocalBadgeCount(2);

    // and 1 purchase
    await volumeByIsbnPage.gotoByIsbn(SEED_ISBN);
    await volumeByIsbnPage.markPurchased();
    await header.expectLocalBadgeCount(3);

    // (c) Login (Stage Z primes `/.auth/me`)
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    // (d) Auto-prompt
    await mergeDialog.expectVisible();
    await mergeDialog.expectCounts(2, 1);

    // (e) Accept → toast + IndexedDB cleared
    await mergeDialog.accepting();
    await toast.expectVisibleWithText(/3\s*件.*引き継/);
    await mergeDialog.expectClosed();
    await header.expectLocalBadgeCount(0);
  });
});

test.describe('merge flow — discard', () => {
  test.skip(true, SKIP_REASON);

  test('discard wipes IndexedDB without calling /api/me/*', async ({
    homePage,
    header,
    searchPage,
    loginPage,
    mergeDialog,
    page,
  }) => {
    await homePage.goto();
    await searchPage.gotoWith(SEED_QUERY, 'series');
    await searchPage.addSeriesToSubscriptions(0);
    await header.expectLocalBadgeCount(1);

    // Fail the test if any /api/me/* mutation slips through after discard.
    const blockedCalls: string[] = [];
    await page.route('**/api/me/**', (route) => {
      const req = route.request();
      if (req.method() !== 'GET') blockedCalls.push(`${req.method()} ${req.url()}`);
      return route.continue();
    });

    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();
    await mergeDialog.expectVisible();
    await mergeDialog.discarding();
    await mergeDialog.expectClosed();
    await header.expectLocalBadgeCount(0);

    expect(blockedCalls, JSON.stringify(blockedCalls)).toEqual([]);
  });
});

test.describe('merge flow — later (snooze)', () => {
  test.skip(true, SKIP_REASON);

  test('snooze keeps IndexedDB; manual trigger available from /settings', async ({
    homePage,
    header,
    searchPage,
    loginPage,
    mergeDialog,
    settingsPage,
    toast,
  }) => {
    await homePage.goto();
    await searchPage.gotoWith(SEED_QUERY, 'series');
    await searchPage.addSeriesToSubscriptions(0);
    await searchPage.addSeriesToSubscriptions(1);
    await header.expectLocalBadgeCount(2);

    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();
    await mergeDialog.expectVisible();
    await mergeDialog.snoozing();
    await mergeDialog.expectClosed();

    // IndexedDB preserved.
    await header.expectLocalBadgeCount(2);

    // Manual trigger surfaces in /settings.
    await settingsPage.goto();
    await settingsPage.expectLoaded();
    await settingsPage.expectMergeRowVisible();
    await settingsPage.openMergePrompt();
    await mergeDialog.expectVisible();
    await mergeDialog.expectCounts(2, 0);
    await mergeDialog.accepting();
    await toast.expectVisibleWithText(/2\s*件.*引き継/);
    await header.expectLocalBadgeCount(0);
  });
});
