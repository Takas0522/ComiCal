/**
 * Home smoke spec.
 *
 * SKIP RATIONALE: Phase 1 E2E ships structurally-complete specs. The actual
 * Angular SSR + Functions + WireMock stack isn't wired into CI yet, so each
 * test wraps assertions in `test.skip(true, ...)` to keep the suite GREEN
 * locally while Playwright still validates spec syntax via `--list`. Stage Z
 * will flip the skip flag.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';

test.describe('home', () => {
  test.skip(true, SKIP_REASON);

  test('renders hero and at least one upcoming volume (or empty state)', async ({
    homePage,
    header,
  }) => {
    await homePage.goto();
    await header.expectVisible();
    await homePage.expectHeroVisible();
    await homePage.expectUpcomingSectionVisible();
    await homePage.expectAtLeastNUpcomingVolumes(1);
  });

  test('home page passes axe a11y sweep', async ({ homePage, axeBuilder }) => {
    await homePage.goto();
    const results = await axeBuilder().analyze();
    expect(results.violations).toEqual([]);
  });
});
