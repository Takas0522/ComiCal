/**
 * QR sync — issue (Device A) + redeem (Device B simulated in same browser).
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 *
 * Per docs/specs/oo-init/13-ux-a11y-i18n.md, the redeem token has a 5-min
 * TTL and is single-use (TokenAlreadyConsumed on second redeem). The QR
 * dialog also exposes the plaintext token + a Copy button (toast: 「コピーしました」).
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';

test.describe('QR sync — happy path', () => {
  test.skip(true, SKIP_REASON);

  test('issue → copy → redeem succeeds', async ({
    homePage,
    header,
    loginPage,
    settingsPage,
    syncPage,
    toast,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();
    await header.expectAuthenticated();

    await settingsPage.goto();
    await settingsPage.issueSyncToken();
    await settingsPage.expectSyncDialogVisible();
    await settingsPage.expectSyncCountdownVisible();

    const token = await settingsPage.getSyncToken();
    expect(token, 'token must be a non-empty plaintext string').not.toEqual('');

    await settingsPage.copySyncToken();
    await toast.expectVisibleWithText(/コピーしました/);

    await settingsPage.closeSyncDialog();

    // Same browser context, simulating Device B with the same login.
    await syncPage.redeem(token);
    await syncPage.expectSuccess();
    await toast.expectVisibleWithText(/同期(完了|しました)/);
  });
});

test.describe('QR sync — re-redeem (TokenAlreadyConsumed)', () => {
  test.skip(true, SKIP_REASON);

  test('second redeem of the same token surfaces a specific error', async ({
    homePage,
    header,
    loginPage,
    settingsPage,
    syncPage,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();

    await settingsPage.goto();
    await settingsPage.issueSyncToken();
    const token = await settingsPage.getSyncToken();
    await settingsPage.closeSyncDialog();

    await syncPage.redeem(token);
    await syncPage.expectSuccess();

    await syncPage.redeem(token);
    await syncPage.expectError(/(消費|consumed|already)/i);
  });
});

test.describe('QR sync — invalid / expired token', () => {
  test.skip(true, SKIP_REASON);

  test('fake token surfaces specific error message', async ({
    homePage,
    header,
    loginPage,
    syncPage,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();

    await syncPage.redeem('this-is-not-a-real-token-0000');
    await syncPage.expectError();
    const msg = await syncPage.getErrorMessage();
    expect(msg.length).toBeGreaterThan(0);
  });

  test('missing token shows the missing-token state', async ({
    homePage,
    header,
    loginPage,
    syncPage,
  }) => {
    await homePage.goto();
    await header.clickLogin();
    await loginPage.clickSignIn();

    await syncPage.gotoWithoutToken();
    await syncPage.expectMissingToken();
  });
});
