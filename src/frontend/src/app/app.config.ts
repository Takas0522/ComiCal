import {
  ApplicationConfig,
  EnvironmentInjector,
  PLATFORM_ID,
  inject,
  provideEnvironmentInitializer,
  provideZonelessChangeDetection,
  effect,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withPreloading,
  PreloadAllModules,
} from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { routes } from './app.routes';
import { authInterceptor } from './core/http/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { transferStateInterceptor } from './core/http/transfer-state.interceptor';
import { AuthService } from './core/auth/auth.service';
import { MergeService } from './core/merge';

interface IdleWindow {
  requestIdleCallback?: (cb: () => void, opts?: { timeout: number }) => number;
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
      // Preload all lazy routes after the initial render so that subsequent
      // navigations (search, calendar, settings, …) are instant. Initial bundle
      // is unaffected; preloads start once Angular reports app stable.
      withPreloading(PreloadAllModules),
    ),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, transferStateInterceptor, errorInterceptor]),
    ),
    provideClientHydration(withEventReplay()),
    provideEnvironmentInitializer(() => {
      const platformId = inject(PLATFORM_ID);
      if (!isPlatformBrowser(platformId)) return;
      const auth = inject(AuthService);
      const merge = inject(MergeService);
      let previous = auth.isAuthenticated();
      effect(() => {
        const current = auth.isAuthenticated();
        if (!previous && current && merge.shouldPrompt()) {
          merge.openPrompt();
        }
        previous = current;
      });
    }),
    provideEnvironmentInitializer(() => {
      const platformId = inject(PLATFORM_ID);
      if (!isPlatformBrowser(platformId)) return;
      // Capture the injector synchronously so the deferred async block can
      // resolve services without being inside an injection context.
      const injector = inject(EnvironmentInjector);
      // Defer AppInsights bootstrap to the first idle frame **and** dynamically
      // import the SDK so the ~180 kB `@microsoft/applicationinsights-web`
      // bundle never lands in the initial chunk. The SDK is only needed for
      // post-LCP telemetry, so this trades a tiny startup delay for a much
      // smaller TTI-critical bundle. Falls back to setTimeout when
      // requestIdleCallback is unavailable (Safari / SSR-replayed paths).
      const w = (typeof window !== 'undefined' ? window : null) as (Window & IdleWindow) | null;
      const start = (): void => {
        void import('./core/observability/application-insights.service').then(
          ({ ApplicationInsightsService }) => {
            injector.get(ApplicationInsightsService).initialize();
          },
        );
      };
      if (w?.requestIdleCallback) {
        w.requestIdleCallback(start, { timeout: 2000 });
      } else {
        setTimeout(start, 1500);
      }
    }),
  ],
};
