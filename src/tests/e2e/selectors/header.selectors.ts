/**
 * Shared selectors for cross-cutting layout components (header, footer, nav).
 */
export const HEADER = {
  root: 'app-header',
  appTitle: 'app-header-title',
  loginButton: 'app-header-login-button',
  navHome: 'nav-home',
  navSearch: 'nav-search',
  skipToContent: 'skip-to-content',
  searchBarInput: 'search-bar-input',
  searchBarSubmit: 'search-bar-submit',
  searchBar: 'search-bar',
} as const;
