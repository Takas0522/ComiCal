/**
 * Authenticated → anonymous logout flow.
 *
 * SKIP RATIONALE: see specs/auth-login.spec.ts.
 *
 * Flow under test:
 *   1. Authenticated header is visible (precondition: SWA mock auth).
 *   2. Click 「ログアウト」 → SWA `/.auth/logout?post_logout_redirect_uri=/`
 *      → page reloads at `/`.
 *   3. Header reverts to anonymous (login link, no user-name, no logout).
 */
import { test } from '../fixtures/test';

const SKIP_REASON = 'Pending Stage Z app deploy (SWA mock auth + Functions)';

test.describe('auth — logout', () => {
  test.skip(true, SKIP_REASON);

  test('authenticated → click ログアウト → header reverts to anonymous', async ({
    homePage,
    header,
    page,
  }) => {
    await homePage.goto();
    // Precondition: the test runner has primed `/.auth/me` with a
    // userDetails payload before the spec body runs (Stage Z fixture).
    await header.expectAuthenticated();

    await header.clickLogout();
    await page.waitForURL('**/');
    await header.expectAnonymous();
  });
});
