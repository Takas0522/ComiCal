import { inject, InjectionToken, makeEnvironmentProviders } from '@angular/core';
import createClient, { type Client } from 'openapi-fetch';
import type { paths } from './schema';

/** Base URL for the ComiCal API. Defaults to `/api/v1`. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  factory: () => '/api/v1',
});

/** Typed openapi-fetch client for the ComiCal API. */
export type ComiCalApiClient = Client<paths>;

export const API_CLIENT = new InjectionToken<ComiCalApiClient>('API_CLIENT', {
  factory: () => {
    const baseUrl = inject(API_BASE_URL);
    return createClient<paths>({ baseUrl });
  },
});

/**
 * Provides the typed API client in the Angular DI tree.
 * Optionally override the base URL (e.g., for testing or staging).
 *
 * @example
 * // app.config.ts
 * provideApiClient()
 *
 * // with custom base URL
 * provideApiClient({ baseUrl: 'https://api.example.com/v1' })
 */
export function provideApiClient(options?: { baseUrl?: string }) {
  const providers = options?.baseUrl ? [{ provide: API_BASE_URL, useValue: options.baseUrl }] : [];
  return makeEnvironmentProviders(providers);
}
