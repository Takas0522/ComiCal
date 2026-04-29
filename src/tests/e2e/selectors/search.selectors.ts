/**
 * Selectors for the /search screen.
 * Some registry keys diverge from frontend naming — see _audit.md
 * (e.g. paginationCursor → pagination-load-more).
 */
export const SEARCH = {
  tabList: 'tab-list',
  tabSeries: 'tab-series',
  tabVolumes: 'tab-volumes',
  // Frontend renders the cursor pagination as "pagination-load-more" button.
  paginationCursor: 'pagination-load-more',
  seriesSearchResults: 'series-search-results',
  volumeSearchResults: 'volume-search-results',
  seriesEmpty: 'series-empty',
  volumeEmpty: 'volume-empty',
  seriesSkeletons: 'series-skeletons',
  volumeSkeletons: 'volume-skeletons',
  seriesCard: 'series-card',
  volumeCard: 'volume-card',
} as const;
