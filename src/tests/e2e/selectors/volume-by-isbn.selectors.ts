/**
 * Selectors for /volumes/by-isbn/:isbn.
 *
 * Registry keys diverge from frontend testids — see _audit.md:
 *   - volumeDetail      → volume-by-isbn-card  (renamed)
 *   - volumeIsbn        → volume-by-isbn-isbn  (renamed)
 *   - volumeReleaseDate → volume-by-isbn-release (renamed)
 *   - volumeSeriesLink  → volume-by-isbn-series-link (renamed)
 */
export const VOLUME_BY_ISBN = {
  loading: 'volume-by-isbn-loading',
  volumeDetail: 'volume-by-isbn-card',
  volumeIsbn: 'volume-by-isbn-isbn',
  volumeName: 'volume-by-isbn-volume',
  volumeReleaseDate: 'volume-by-isbn-release',
  volumeSeriesLink: 'volume-by-isbn-series-link',
} as const;
