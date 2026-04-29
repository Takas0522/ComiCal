import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { PLATFORM_ID, TransferState, inject, makeStateKey } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { of, tap } from 'rxjs';

/**
 * Cache GET HTTP responses produced during SSR into TransferState so the
 * browser can re-hydrate them without re-fetching.
 */
export const transferStateInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') {
    return next(req);
  }

  const transferState = inject(TransferState);
  const platformId = inject(PLATFORM_ID);
  const key = makeStateKey<unknown>(`http:${req.urlWithParams}`);

  if (transferState.hasKey(key)) {
    const cached = transferState.get(key, null);
    transferState.remove(key);
    return of(new HttpResponse({ body: cached, status: 200 }));
  }

  return next(req).pipe(
    tap((event) => {
      if (isPlatformServer(platformId) && event instanceof HttpResponse) {
        transferState.set(key, event.body);
      }
    }),
  );
};
