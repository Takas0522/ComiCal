/**
 * Selectors for authentication entry/exit points.
 *
 * The actual SWA (`/.auth/login/aadb2c`, `/.auth/logout`, `/.auth/me`)
 * endpoints are out-of-app surfaces — see specs/auth-*.spec.ts for how
 * we mock them via the SWA emulator. Here we centralize only the
 * in-app data-testid strings.
 */
export const AUTH = {
  // Login page
  loginAadb2c: 'login-aadb2c',
  loginError: 'login-error',
  loginLede: 'login-lede',
  loginPlaceholder: 'login-placeholder',
  // Header authenticated/anonymous slots
  headerAuth: 'header-auth',
  headerLogin: 'header-login',
  headerLogout: 'header-logout',
  headerUserName: 'header-user-name',
} as const;
