import { HttpClient } from '@angular/common/http';
import {
  Injectable,
  Signal,
  computed,
  inject,
  provideAppInitializer,
  signal,
} from '@angular/core';
import { Observable, catchError, firstValueFrom, of, tap } from 'rxjs';

import {
  FEATURE_FLAG_NAMES,
  type FeatureFlagMap,
  type FeatureFlagName,
} from './feature-flag.types';

const FEATURE_FLAGS_ENDPOINT = '/api/feature-flags';

function emptyFlagMap(): FeatureFlagMap {
  return Object.freeze(
    Object.fromEntries(FEATURE_FLAG_NAMES.map((name) => [name, false])),
  ) as FeatureFlagMap;
}

/**
 * Signal-based feature flag store.
 *
 * - Fetches `/api/feature-flags` once at app bootstrap (via {@link provideFeatureFlags}).
 * - Exposes `isEnabled(name)` as a `Signal<boolean>` so templates / `computed`
 *   chains stay reactive when flags are reloaded.
 * - On fetch failure, all known flags default to `false` (fail-closed).
 *
 * TODO: invoke provideFeatureFlags() from app.config.ts (see README.md in
 * this directory). Wiring is intentionally left out here to avoid a merge
 * conflict with the in-flight i18n provider work.
 */
@Injectable({ providedIn: 'root' })
export class FeatureFlagService {
  private readonly http = inject(HttpClient);
  private readonly _flags = signal<FeatureFlagMap>(emptyFlagMap());

  readonly flags = computed(() => this._flags());

  loadFlags(): Observable<FeatureFlagMap> {
    return this.http.get<FeatureFlagMap>(FEATURE_FLAGS_ENDPOINT).pipe(
      tap((map) => this._flags.set(Object.freeze({ ...(map ?? {}) }) as FeatureFlagMap)),
      catchError(() => {
        const fallback = emptyFlagMap();
        this._flags.set(fallback);
        return of(fallback);
      }),
    );
  }

  isEnabled(name: FeatureFlagName | string): Signal<boolean> {
    return computed(() => this._flags()[name] === true);
  }
}

/**
 * Returns an `EnvironmentProviders` array that fetches the feature flag map
 * during app bootstrap. Add this to `appConfig.providers` once the i18n
 * provider work has merged.
 */
export function provideFeatureFlags() {
  return provideAppInitializer(() => {
    const service = inject(FeatureFlagService);
    return firstValueFrom(service.loadFlags());
  });
}
