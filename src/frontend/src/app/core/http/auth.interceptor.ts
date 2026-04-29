import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Forward Static Web Apps `/.auth/me` session token to the Functions API.
 *
 * TODO (Phase 1): read the SWA principal cookie / `x-ms-client-principal`
 * header (server) or fetch `/.auth/me` (browser) and attach an Authorization
 * header to outbound API requests. For now this is a no-op pass-through so
 * the interceptor pipeline is wired and unit-testable.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req);
};
