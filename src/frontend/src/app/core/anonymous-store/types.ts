/**
 * Types for the local-only (pre-login) IndexedDB store.
 */

export type PurchaseState = 'bought' | 'reading' | 'finished';

export interface AnonymousSubscription {
  readonly seriesId: string;
  readonly addedAt: string;
  readonly updatedAt: string;
  readonly notes?: string;
}

export interface AnonymousPurchase {
  readonly volumeId: string;
  readonly seriesId: string;
  readonly isbn13: string;
  readonly state: PurchaseState;
  readonly purchasedAt?: string;
  readonly updatedAt: string;
  readonly notes?: string;
}

export const ANONYMOUS_EXPORT_SCHEMA_VERSION = 1 as const;

export interface AnonymousExport {
  readonly schemaVersion: typeof ANONYMOUS_EXPORT_SCHEMA_VERSION;
  readonly exportedAt: string;
  readonly subscriptions: readonly AnonymousSubscription[];
  readonly purchases: readonly AnonymousPurchase[];
}
