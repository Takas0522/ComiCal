import { HttpInterceptorFn } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID, inject } from '@angular/core';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  // On browser, SWA injects x-ms-client-principal automatically via cookie
  // For API requests, add credentials
  if (isPlatformBrowser(platformId) && req.url.startsWith('/api/')) {
    return next(req.clone({ withCredentials: true }));
  }
  return next(req);
};
