/**
 * Selectors for the /series/:id (series detail) screen.
 *
 * Several requested registry keys do not have a 1:1 data-testid counterpart
 * in the Phase 1 frontend. See _audit.md for the rename map and gaps:
 *   - seriesTitle      → series-detail-title (renamed)
 *   - seriesAuthor     → MISSING (no testid in current page)
 *   - seriesPublisher  → MISSING (no testid in current page)
 */
export const SERIES_DETAIL = {
  loading: 'series-detail-loading',
  header: 'series-detail-header',
  seriesTitle: 'series-detail-title',
  status: 'series-detail-status',
  seriesVolumeList: 'series-volume-list',
  seriesVolumeListEmpty: 'series-volume-list-empty',
  seriesVolumeListMonth: 'series-volume-list-month',
  // Gap markers — the frontend does not yet expose these as testids.
  // Specs that depend on them must use test.fixme() or test.skip().
  seriesAuthor: 'series-detail-author',
  seriesPublisher: 'series-detail-publisher',
} as const;
