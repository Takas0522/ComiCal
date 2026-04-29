/**
 * Cross-page accessibility sweep.
 *
 * SKIP RATIONALE: see specs/home.spec.ts.
 *
 * Sweeps the four primary Phase 1 surfaces with axe-core. Any axe finding
 * tagged `serious` or `critical` fails the spec; lesser tags are reported
 * but tolerated to keep the gate signal high.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';

const SEED_SERIES_ID = '00000000-0000-0000-0000-000000000001';
const SEED_ISBN = '9784088838212';

const PAGES: ReadonlyArray<{ name: string; path: string }> = [
  { name: 'home', path: '/' },
  { name: 'search', path: '/search?q=ワンピース&tab=series' },
  { name: 'series-detail', path: `/series/${SEED_SERIES_ID}` },
  { name: 'volume-by-isbn', path: `/volumes/by-isbn/${SEED_ISBN}` },
];

test.describe('a11y sweep', () => {
  test.skip(true, SKIP_REASON);

  for (const target of PAGES) {
    test(`${target.name} has no serious/critical axe violations`, async ({
      page,
      axeBuilder,
    }) => {
      await page.goto(target.path);
      const results = await axeBuilder().analyze();
      const blocking = results.violations.filter((v) =>
        v.impact === 'serious' || v.impact === 'critical',
      );
      expect(blocking, JSON.stringify(blocking, null, 2)).toEqual([]);
    });
  }
});
