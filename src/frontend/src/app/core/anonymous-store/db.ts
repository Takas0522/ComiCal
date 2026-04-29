import type { DBSchema, IDBPDatabase } from 'idb';

import type { AnonymousPurchase, AnonymousSubscription } from './types';

export const DB_NAME = 'comical-anon';
export const DB_VERSION = 1;

export const STORE_SUBSCRIPTIONS = 'subscriptions';
export const STORE_PURCHASES = 'purchases';
export const STORE_META = 'meta';

export interface MetaRecord {
  readonly key: string;
  readonly value: string;
}

export interface ComiCalSchema extends DBSchema {
  [STORE_SUBSCRIPTIONS]: {
    key: string;
    value: AnonymousSubscription;
    indexes: { 'by-updatedAt': string };
  };
  [STORE_PURCHASES]: {
    key: string;
    value: AnonymousPurchase;
    indexes: { 'by-updatedAt': string; 'by-seriesId': string };
  };
  [STORE_META]: {
    key: string;
    value: MetaRecord;
  };
}

export type ComiCalDb = IDBPDatabase<ComiCalSchema>;

export function isIndexedDbAvailable(): boolean {
  return typeof indexedDB !== 'undefined';
}

let dbPromise: Promise<ComiCalDb> | null = null;
let warnedNoIdb = false;

/** Reset cached singleton — exposed for tests. Also closes the open connection. */
export async function __resetComiCalDbForTests(): Promise<void> {
  const cur = dbPromise;
  dbPromise = null;
  warnedNoIdb = false;
  if (cur) {
    try {
      const db = await cur;
      db.close();
    } catch {
      // ignore
    }
  }
}

/**
 * Idempotently open the anonymous-store DB. Returns `null` when running
 * server-side (no IndexedDB). Callers MUST handle that.
 */
export async function openComiCalDb(): Promise<ComiCalDb | null> {
  if (!isIndexedDbAvailable()) {
    if (!warnedNoIdb) {
      warnedNoIdb = true;
      console.warn(
        '[anonymous-store] IndexedDB is unavailable (likely SSR). Operations are no-ops.',
      );
    }
    return null;
  }
  if (dbPromise) {
    return dbPromise;
  }
  const { openDB } = await import('idb');
  dbPromise = openDB<ComiCalSchema>(DB_NAME, DB_VERSION, {
    upgrade(db, oldVersion) {
      if (oldVersion < 1) {
        const subs = db.createObjectStore(STORE_SUBSCRIPTIONS, {
          keyPath: 'seriesId',
        });
        subs.createIndex('by-updatedAt', 'updatedAt');

        const purchases = db.createObjectStore(STORE_PURCHASES, {
          keyPath: 'volumeId',
        });
        purchases.createIndex('by-updatedAt', 'updatedAt');
        purchases.createIndex('by-seriesId', 'seriesId');

        db.createObjectStore(STORE_META, { keyPath: 'key' });
      }
    },
  });
  return dbPromise;
}
