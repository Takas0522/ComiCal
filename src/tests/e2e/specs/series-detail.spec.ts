/**
 * Series detail smoke spec.
 *
 * SKIP RATIONALE: see specs/home.spec.ts.
 *
 * Additional note: the Phase 1 frontend does not yet expose `series-author`
 * or `series-publisher` data-testid attributes (see selectors/_audit.md).
 * Those assertions are wrapped in `test.fixme()` so they are listed but
 * marked as known-failing until the frontend lands them.
 *
 * The series id below is a placeholder GUID. The real WireMock-seeded id
 * is unknown to this task — the spec uses a deterministic placeholder and
 * is skipped wholesale until Stage Z plumbing decides on a stable seed.
 */
import { test } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';
const SEED_SERIES_ID = '00000000-0000-0000-0000-000000000001';

test.describe('series detail', () => {
  test('renders title and volume list for a seeded series', async ({
    seriesDetailPage,
  }) => {
    test.skip(true, SKIP_REASON);
    await seriesDetailPage.gotoById(SEED_SERIES_ID);
    await seriesDetailPage.expectSeriesTitle(/.+/);
    await seriesDetailPage.expectStatusVisible();
    await seriesDetailPage.expectVolumeListNotEmpty();
  });

  test('renders author and publisher metadata', async ({ seriesDetailPage }) => {
    test.fixme(true, 'Frontend does not yet expose series-author / series-publisher testids — see selectors/_audit.md');
    await seriesDetailPage.gotoById(SEED_SERIES_ID);
    await seriesDetailPage.expectAuthorVisible();
    await seriesDetailPage.expectPublisherVisible();
  });
});
