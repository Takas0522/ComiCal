/**
 * Account deletion (danger zone).
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 *
 * Per docs/specs/oo-init/13-ux-a11y-i18n.md the delete flow requires:
 *   1. Section is collapsed by default; user must expand.
 *   2. Confirmation field must contain the literal 「削除」.
 *   3. Primary in-section button opens a role="alertdialog".
 *   4. Confirming the alertdialog calls DELETE /api/me, which logs the
 *      user out via SWA `/.auth/logout` and returns to `/`.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';
const CONFIRMATION_PHRASE = '削除';

test.describe('account — delete', () => {
  test.skip(true, SKIP_REASON);

  test('login → expand → type 削除 → confirm → logout to anonymous /', async ({
    homePage,
    header,
    loginPage,
    settingsPage,
    page,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    await settingsPage.goto();
    await settingsPage.expandDeleteSection();

    // Until the confirmation phrase is typed, the in-section button stays
    // disabled to prevent accidental click-throughs.
    await settingsPage.expectDeleteButtonDisabled();
    await settingsPage.typeDeletionConfirmation(CONFIRMATION_PHRASE);
    await settingsPage.expectDeleteButtonEnabled();

    await settingsPage.clickDelete();
    const deletePromise = page.waitForResponse(
      (r) => /\/api\/me$/.test(r.url()) && r.request().method() === 'DELETE',
    );
    await settingsPage.confirmDeletion();
    const del = await deletePromise;
    expect(del.ok()).toBeTruthy();

    // SWA logout → back to `/` as anonymous.
    await page.waitForURL('**/');
    await header.expectAnonymous();
  });

  test('typing the wrong phrase keeps the delete button disabled', async ({
    homePage,
    header,
    loginPage,
    settingsPage,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();

    await settingsPage.goto();
    await settingsPage.expandDeleteSection();
    await settingsPage.typeDeletionConfirmation('あいうえお');
    await settingsPage.expectDeleteButtonDisabled();
  });

  test('after deletion, re-login surfaces a fresh account (no subscriptions)', async ({
    homePage,
    header,
    loginPage,
    page,
  }) => {
    // Precondition: previous test (or a Stage Z fixture) deleted the user.
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    const list = await page.request.get('/api/me/subscriptions');
    expect(list.ok()).toBeTruthy();
    const body = await list.json();
    const items = body.items ?? body;
    expect(Array.isArray(items)).toBeTruthy();
    expect(items).toHaveLength(0);
  });
});
