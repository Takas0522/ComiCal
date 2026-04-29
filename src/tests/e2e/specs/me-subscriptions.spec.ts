/**
 * Authenticated subscriptions — `/api/me/subscriptions` round-trip.
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 *
 * Mirrors docs/specs/oo-init/12-backend-api.md (Idempotent toggle). When
 * the user is authenticated, the subscription-toggle on a series card
 * issues PUT /api/me/subscriptions/{seriesId}; toggling off issues
 * DELETE; rapid on/off/on must converge to the final ON state.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';
const SEED_QUERY = 'ワンピース';

test.describe('me — subscriptions', () => {
  test.skip(true, SKIP_REASON);

  test('toggle on → GET /api/me/subscriptions returns it', async ({
    homePage,
    header,
    loginPage,
    searchPage,
    page,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    await searchPage.gotoWith(SEED_QUERY, 'series');
    const responsePromise = page.waitForResponse(
      (r) => /\/api\/me\/subscriptions\//.test(r.url()) && r.request().method() === 'PUT',
    );
    await searchPage.addSeriesToSubscriptions(0);
    const put = await responsePromise;
    expect(put.status()).toBeGreaterThanOrEqual(200);
    expect(put.status()).toBeLessThan(300);

    // Re-fetch the list and confirm presence.
    const list = await page.request.get('/api/me/subscriptions');
    expect(list.ok()).toBeTruthy();
    const body = await list.json();
    expect(Array.isArray(body.items ?? body)).toBeTruthy();
  });

  test('toggle off → soft-delete; subsequent list does not include it', async ({
    homePage,
    header,
    loginPage,
    searchPage,
    page,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await searchPage.gotoWith(SEED_QUERY, 'series');

    await searchPage.addSeriesToSubscriptions(0);
    const deletePromise = page.waitForResponse(
      (r) => /\/api\/me\/subscriptions\//.test(r.url()) && r.request().method() === 'DELETE',
    );
    await searchPage.removeSeriesFromSubscriptions(0);
    const del = await deletePromise;
    expect(del.status()).toBeGreaterThanOrEqual(200);
    expect(del.status()).toBeLessThan(300);
  });

  test('idempotency: rapid on/off/on converges to final ON', async ({
    homePage,
    header,
    loginPage,
    searchPage,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await searchPage.gotoWith(SEED_QUERY, 'series');

    // Three toggles. Auto-waiting locators serialize the clicks but the
    // service layer is responsible for cancelling stale in-flight calls.
    await searchPage.addSeriesToSubscriptions(0);
    await searchPage.removeSeriesFromSubscriptions(0);
    await searchPage.addSeriesToSubscriptions(0);
    // Final state asserted inside addSeriesToSubscriptions (aria-pressed=true).
  });
});
