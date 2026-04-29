/**
 * Authenticated login flow.
 *
 * SKIP RATIONALE: identical to Phase 1 specs — the SWA + Functions stack
 * isn't wired into CI yet. Stage Z will provision a SWA emulator with a
 * mocked `/.auth/login/aadb2c` and `/.auth/me` endpoint and flip the skip
 * flag.
 *
 * Flow under test:
 *   1. Anonymous user lands on `/` → header shows 「ログイン」 link.
 *   2. Click 「ログイン」 → routed to /login → click `login-aadb2c`.
 *   3. SWA redirects to `/.auth/login/aadb2c?post_login_redirect_uri=/` →
 *      mock auth UI accepts → callback → `/.auth/me` returns userDetails.
 *   4. Header now shows `header-user-name` + 「ログアウト」 link.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';

test.describe('auth — login', () => {
  test.skip(true, SKIP_REASON);

  test('anonymous → click ログイン → SWA mock auth → header shows userDetails', async ({
    homePage,
    loginPage,
    header,
  }) => {
    await homePage.goto();
    await header.expectAnonymous();

    await header.clickLogin();
    await loginPage.expectVisible();
    await loginPage.clickSignIn();

    // SWA emulator's mock UI accepts a default `userDetails` and bounces
    // back to the post_login_redirect_uri (/), so we just wait for the
    // header to flip into the authenticated state.
    await header.expectAuthenticated();
  });

  test('login page exposes Entra External ID CTA', async ({ loginPage }) => {
    await loginPage.goto();
    await loginPage.expectVisible();
  });

  test('login page passes axe a11y sweep', async ({ loginPage, axeBuilder }) => {
    await loginPage.goto();
    const results = await axeBuilder().analyze();
    const blocking = results.violations.filter(
      (v) => v.impact === 'serious' || v.impact === 'critical',
    );
    expect(blocking).toEqual([]);
  });
});
