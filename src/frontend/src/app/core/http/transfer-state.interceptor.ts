import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject, PLATFORM_ID, makeStateKey, TransferState } from '@angular/core';
import { isPlatformBrowser, isPlatformServer } from '@angular/common';
import { of, tap } from 'rxjs';

export const transferStateInterceptor: HttpInterceptorFn = (req, next) => {
  const transferState = inject(TransferState);
  const platformId = inject(PLATFORM_ID);

  if (req.method !== 'GET') return next(req);

  const stateKey = makeStateKey<unknown>(req.urlWithParams);

  if (isPlatformBrowser(platformId)) {
    const cached = transferState.get(stateKey, null);
    if (cached) {
      transferState.remove(stateKey);
      return of(new HttpResponse({ body: cached, status: 200 }));
    }
  }

  return next(req).pipe(
    tap(event => {
      if (isPlatformServer(platformId) && event instanceof HttpResponse) {
        transferState.set(stateKey, event.body);
      }
    }),
  );
};
