/**
 * Centralised data-testid registry for the home screen and shared layout
 * primitives consumed from Home (cards, skeletons, search bar, nav links).
 *
 * Every selector consumed by a Page Object MUST live here — never hard-coded
 * inside pages/ or specs/. See selectors/_audit.md for any naming gaps
 * between the registry keys requested by the test plan and the actual
 * data-testid attributes rendered by the frontend.
 */
export const HOME = {
  hero: 'home-hero',
  upcoming: 'home-upcoming',
  popular: 'home-popular',
  popularEmpty: 'home-popular-empty',
  volumeCard: 'volume-card',
  seriesCard: 'series-card',
  emptyState: 'empty-state',
  skeleton: 'skeleton',
  searchBar: 'search-bar',
  searchBarInput: 'search-bar-input',
  searchBarSubmit: 'search-bar-submit',
  // NOTE: registry keys nav-link-home / nav-link-search were renamed
  // to match the actual frontend attributes nav-home / nav-search.
  // See _audit.md.
  navLinkHome: 'nav-home',
  navLinkSearch: 'nav-search',
  skipToContent: 'skip-to-content',
} as const;
