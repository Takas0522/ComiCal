import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';

import { AnonymousPurchaseRepository } from './anonymous-purchase.repository';
import { AnonymousSubscriptionRepository } from './anonymous-subscription.repository';
import { __resetComiCalDbForTests } from './db';
import {
  AnonymousImportSchemaError,
  AnonymousStoreExportService,
} from './export';
import {
  ANONYMOUS_EXPORT_SCHEMA_VERSION,
  type AnonymousExport,
} from './types';

async function clearStores(): Promise<void> {
  const subs = TestBed.inject(AnonymousSubscriptionRepository);
  const purs = TestBed.inject(AnonymousPurchaseRepository);
  await Promise.all([subs.clear(), purs.clear()]);
}

describe('AnonymousStoreExportService', () => {
  let svc: AnonymousStoreExportService;
  let subs: AnonymousSubscriptionRepository;
  let purs: AnonymousPurchaseRepository;

  beforeEach(async () => {
    await __resetComiCalDbForTests();
    TestBed.configureTestingModule({});
    svc = TestBed.inject(AnonymousStoreExportService);
    subs = TestBed.inject(AnonymousSubscriptionRepository);
    purs = TestBed.inject(AnonymousPurchaseRepository);
    await Promise.all([subs.list(), purs.list()]);
    await clearStores();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
  });

  it('round-trips an empty export', async () => {
    const exp = await svc.exportAll();
    expect(exp.schemaVersion).toBe(ANONYMOUS_EXPORT_SCHEMA_VERSION);
    expect(exp.subscriptions).toEqual([]);
    expect(exp.purchases).toEqual([]);
  });

  it('exports current data', async () => {
    await subs.add('s1');
    await purs.upsert({
      volumeId: 'v1',
      seriesId: 's1',
      isbn13: '9784000000001',
      state: 'bought',
      updatedAt: '2026-04-01T00:00:00.000Z',
    });
    const exp = await svc.exportAll();
    expect(exp.subscriptions).toHaveLength(1);
    expect(exp.purchases).toHaveLength(1);
  });

  it('rejects unknown schema version', async () => {
    const bad = {
      schemaVersion: 999,
      exportedAt: new Date().toISOString(),
      subscriptions: [],
      purchases: [],
    } as unknown as AnonymousExport;
    await expect(svc.importAll(bad)).rejects.toBeInstanceOf(
      AnonymousImportSchemaError,
    );
  });

  it('merges with last-write-wins on updatedAt', async () => {
    await subs.add('s1');
    const local = (await subs.list()).find((s) => s.seriesId === 's1')!;
    const olderUpdate = '2020-01-01T00:00:00.000Z';
    const newerUpdate = '2099-01-01T00:00:00.000Z';

    const incoming: AnonymousExport = {
      schemaVersion: ANONYMOUS_EXPORT_SCHEMA_VERSION,
      exportedAt: new Date().toISOString(),
      subscriptions: [
        // Conflicting older — must NOT override local (which is newer).
        { seriesId: 's1', addedAt: olderUpdate, updatedAt: olderUpdate, notes: 'old' },
        // New series — must be inserted.
        { seriesId: 's2', addedAt: newerUpdate, updatedAt: newerUpdate },
      ],
      purchases: [
        {
          volumeId: 'v1',
          seriesId: 's1',
          isbn13: '9784000000001',
          state: 'finished',
          updatedAt: newerUpdate,
        },
      ],
    };
    await svc.importAll(incoming);

    const all = await subs.list();
    const s1 = all.find((s) => s.seriesId === 's1')!;
    expect(s1.updatedAt).toBe(local.updatedAt);
    expect(s1.notes).toBeUndefined();
    expect(all.map((s) => s.seriesId).sort()).toEqual(['s1', 's2']);

    const v1 = await purs.getByVolumeId('v1');
    expect(v1?.state).toBe('finished');
  });

  it('newer incoming overrides older local', async () => {
    const oldUpdate = '2020-01-01T00:00:00.000Z';
    await purs.upsert({
      volumeId: 'v1',
      seriesId: 's1',
      isbn13: '9784000000001',
      state: 'bought',
      updatedAt: oldUpdate,
    });
    const newUpdate = '2099-01-01T00:00:00.000Z';
    await svc.importAll({
      schemaVersion: ANONYMOUS_EXPORT_SCHEMA_VERSION,
      exportedAt: new Date().toISOString(),
      subscriptions: [],
      purchases: [
        {
          volumeId: 'v1',
          seriesId: 's1',
          isbn13: '9784000000001',
          state: 'finished',
          updatedAt: newUpdate,
        },
      ],
    });
    expect((await purs.getByVolumeId('v1'))?.state).toBe('finished');
  });
});
