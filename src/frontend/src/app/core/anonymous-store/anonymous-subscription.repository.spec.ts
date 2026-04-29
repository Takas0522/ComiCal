import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';

import { AnonymousSubscriptionRepository } from './anonymous-subscription.repository';
import { DB_NAME, __resetComiCalDbForTests } from './db';

async function deleteDb(): Promise<void> {
  await new Promise<void>((resolve) => {
    const req = indexedDB.deleteDatabase(DB_NAME);
    req.onsuccess = (): void => resolve();
    req.onerror = (): void => resolve();
    req.onblocked = (): void => resolve();
  });
}

describe('AnonymousSubscriptionRepository', () => {
  let repo: AnonymousSubscriptionRepository;

  beforeEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
    TestBed.configureTestingModule({});
    repo = TestBed.inject(AnonymousSubscriptionRepository);
    // wait for hydrate
    await repo.list();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
  });

  it('add then list returns the entry', async () => {
    await repo.add('s1');
    const all = await repo.list();
    expect(all).toHaveLength(1);
    expect(all[0].seriesId).toBe('s1');
    expect(repo.count()).toBe(1);
  });

  it('has() returns true after add and false after remove', async () => {
    await repo.add('s2');
    expect(await repo.has('s2')).toBe(true);
    await repo.remove('s2');
    expect(await repo.has('s2')).toBe(false);
    expect(repo.count()).toBe(0);
  });

  it('hasSignal reflects mutations', async () => {
    const sig = repo.hasSignal('s3');
    expect(sig()).toBe(false);
    await repo.add('s3');
    expect(sig()).toBe(true);
    await repo.remove('s3');
    expect(sig()).toBe(false);
  });

  it('add is idempotent and preserves addedAt', async () => {
    await repo.add('s4');
    const first = (await repo.list()).find((s) => s.seriesId === 's4')!;
    await new Promise((r) => setTimeout(r, 5));
    await repo.add('s4');
    const second = (await repo.list()).find((s) => s.seriesId === 's4')!;
    expect(second.addedAt).toBe(first.addedAt);
    expect(second.updatedAt >= first.updatedAt).toBe(true);
  });

  it('clear removes all entries', async () => {
    await repo.add('a');
    await repo.add('b');
    await repo.clear();
    expect((await repo.list()).length).toBe(0);
    expect(repo.count()).toBe(0);
  });

  it('persists across repository instances (re-hydration)', async () => {
    await repo.add('persist');
    await __resetComiCalDbForTests();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const fresh = TestBed.inject(AnonymousSubscriptionRepository);
    const all = await fresh.list();
    expect(all.map((s) => s.seriesId)).toContain('persist');
  });
});
