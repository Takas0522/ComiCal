/**
 * Authenticated purchases — `/api/me/purchases` round-trip.
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';
const SEED_ISBN = '9784088838212';

test.describe('me — purchases', () => {
  test.skip(true, SKIP_REASON);

  test('mark purchased → GET /api/me/purchases returns it', async ({
    homePage,
    header,
    loginPage,
    volumeByIsbnPage,
    page,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    await volumeByIsbnPage.gotoByIsbn(SEED_ISBN);
    const putPromise = page.waitForResponse(
      (r) => /\/api\/me\/purchases\//.test(r.url()) && r.request().method() === 'PUT',
    );
    await volumeByIsbnPage.markPurchased();
    const put = await putPromise;
    expect(put.ok()).toBeTruthy();

    const list = await page.request.get('/api/me/purchases');
    expect(list.ok()).toBeTruthy();
  });

  test('toggle off → soft-delete', async ({
    homePage,
    header,
    loginPage,
    volumeByIsbnPage,
    page,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();

    await volumeByIsbnPage.gotoByIsbn(SEED_ISBN);
    await volumeByIsbnPage.markPurchased();

    const deletePromise = page.waitForResponse(
      (r) => /\/api\/me\/purchases\//.test(r.url()) && r.request().method() === 'DELETE',
    );
    await volumeByIsbnPage.unmarkPurchased();
    const del = await deletePromise;
    expect(del.ok()).toBeTruthy();
  });
});
