import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';

import { AnonymousPurchaseRepository } from './anonymous-purchase.repository';
import { DB_NAME, __resetComiCalDbForTests } from './db';
import type { AnonymousPurchase } from './types';

const make = (overrides: Partial<AnonymousPurchase> = {}): AnonymousPurchase => ({
  volumeId: 'v1',
  seriesId: 's1',
  isbn13: '9784000000001',
  state: 'bought',
  updatedAt: new Date().toISOString(),
  ...overrides,
});

async function deleteDb(): Promise<void> {
  await new Promise<void>((resolve) => {
    const req = indexedDB.deleteDatabase(DB_NAME);
    req.onsuccess = (): void => resolve();
    req.onerror = (): void => resolve();
    req.onblocked = (): void => resolve();
  });
}

describe('AnonymousPurchaseRepository', () => {
  let repo: AnonymousPurchaseRepository;

  beforeEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
    TestBed.configureTestingModule({});
    repo = TestBed.inject(AnonymousPurchaseRepository);
    await repo.list();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
  });

  it('upsert + getByVolumeId', async () => {
    await repo.upsert(make());
    expect((await repo.getByVolumeId('v1'))?.state).toBe('bought');
    await repo.upsert(make({ state: 'finished' }));
    expect((await repo.getByVolumeId('v1'))?.state).toBe('finished');
    expect(repo.count()).toBe(1);
  });

  it('listForSeries filters by seriesId', async () => {
    await repo.upsert(make({ volumeId: 'v1', seriesId: 's1' }));
    await repo.upsert(make({ volumeId: 'v2', seriesId: 's1' }));
    await repo.upsert(make({ volumeId: 'v3', seriesId: 's2' }));
    const s1 = await repo.listForSeries('s1');
    expect(s1.map((p) => p.volumeId).sort()).toEqual(['v1', 'v2']);
  });

  it('remove deletes the volume', async () => {
    await repo.upsert(make());
    await repo.remove('v1');
    expect(await repo.getByVolumeId('v1')).toBeNull();
  });

  it('getByVolumeIdSignal reflects upserts', async () => {
    const sig = repo.getByVolumeIdSignal('v1');
    expect(sig()).toBeNull();
    await repo.upsert(make());
    expect(sig()?.state).toBe('bought');
  });

  it('clear empties the store', async () => {
    await repo.upsert(make({ volumeId: 'va' }));
    await repo.upsert(make({ volumeId: 'vb' }));
    await repo.clear();
    expect((await repo.list()).length).toBe(0);
  });
});
