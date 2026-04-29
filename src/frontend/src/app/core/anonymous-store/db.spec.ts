/* eslint-disable @typescript-eslint/no-explicit-any */
import {
  DB_NAME,
  DB_VERSION,
  STORE_PURCHASES,
  STORE_SUBSCRIPTIONS,
  __resetComiCalDbForTests,
  isIndexedDbAvailable,
  openComiCalDb,
} from './db';

describe('anonymous-store/db (SSR-safe path)', () => {
  const originalIndexedDb = (globalThis as any).indexedDB;

  beforeAll(async () => {
    delete (globalThis as any).indexedDB;
    await __resetComiCalDbForTests();
  });
  afterAll(async () => {
    (globalThis as any).indexedDB = originalIndexedDb;
    await __resetComiCalDbForTests();
  });

  it('reports IndexedDB unavailable', () => {
    expect(isIndexedDbAvailable()).toBe(false);
  });

  it('openComiCalDb returns null and warns once', async () => {
    const warn = jest.spyOn(console, 'warn').mockImplementation(() => undefined);
    expect(await openComiCalDb()).toBeNull();
    expect(await openComiCalDb()).toBeNull();
    expect(warn).toHaveBeenCalledTimes(1);
    warn.mockRestore();
  });
});

describe('anonymous-store/db (browser path with fake-indexeddb)', () => {
  beforeAll(async () => {
    await import('fake-indexeddb/auto');
    await __resetComiCalDbForTests();
  });
  afterEach(async () => {
    await __resetComiCalDbForTests();
    await new Promise<void>((resolve, reject) => {
      const req = indexedDB.deleteDatabase(DB_NAME);
      req.onsuccess = (): void => resolve();
      req.onerror = (): void => reject(req.error);
      req.onblocked = (): void => resolve();
    });
  });

  it('opens at the configured version and creates all object stores', async () => {
    const db = await openComiCalDb();
    expect(db).not.toBeNull();
    expect(db!.version).toBe(DB_VERSION);
    expect(db!.objectStoreNames.contains(STORE_SUBSCRIPTIONS)).toBe(true);
    expect(db!.objectStoreNames.contains(STORE_PURCHASES)).toBe(true);
    expect(db!.objectStoreNames.contains('meta')).toBe(true);
  });

  it('caches the open promise (idempotent)', async () => {
    const a = await openComiCalDb();
    const b = await openComiCalDb();
    expect(a).toBe(b);
  });
});
