/**
 * Volume by ISBN smoke spec.
 *
 * SKIP RATIONALE: see specs/home.spec.ts.
 */
import { test } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';
const SEED_ISBN = '9784088838212';

test.describe('volume by ISBN', () => {
  test.skip(true, SKIP_REASON);

  test('renders ISBN and release date for a seeded volume', async ({
    volumeByIsbnPage,
  }) => {
    await volumeByIsbnPage.gotoByIsbn(SEED_ISBN);
    await volumeByIsbnPage.expectCardVisible();
    await volumeByIsbnPage.expectIsbn(SEED_ISBN);
    await volumeByIsbnPage.expectReleaseDateVisible();
    await volumeByIsbnPage.expectSeriesLinkPresent();
  });
});
