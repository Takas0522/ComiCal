import { Injectable, computed, signal, type Signal } from '@angular/core';

import { STORE_PURCHASES, isIndexedDbAvailable, openComiCalDb } from './db';
import type { AnonymousPurchase } from './types';

/**
 * Repository for anonymous (pre-login) volume purchase / reading state.
 *
 * - Keyed by `volumeId`. Indexed by `seriesId` for "all volumes I own of
 *   series X" lookups.
 * - SSR-safe (no-op writes, empty reads).
 */
@Injectable({ providedIn: 'root' })
export class AnonymousPurchaseRepository {
  private readonly _entries = signal<readonly AnonymousPurchase[]>([]);
  private hydrated = false;
  private hydrating: Promise<void> | null = null;

  readonly entries: Signal<readonly AnonymousPurchase[]> =
    this._entries.asReadonly();

  readonly count: Signal<number> = computed(() => this._entries().length);

  constructor() {
    void this.hydrate();
  }

  getByVolumeIdSignal(volumeId: string): Signal<AnonymousPurchase | null> {
    return computed(
      () => this._entries().find((p) => p.volumeId === volumeId) ?? null,
    );
  }

  async list(): Promise<readonly AnonymousPurchase[]> {
    await this.hydrate();
    return this._entries();
  }

  async listForSeries(seriesId: string): Promise<readonly AnonymousPurchase[]> {
    await this.hydrate();
    return this._entries().filter((p) => p.seriesId === seriesId);
  }

  async getByVolumeId(volumeId: string): Promise<AnonymousPurchase | null> {
    await this.hydrate();
    return this._entries().find((p) => p.volumeId === volumeId) ?? null;
  }

  async upsert(purchase: AnonymousPurchase): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    await this.hydrate();
    const db = await openComiCalDb();
    if (!db) return;
    await db.put(STORE_PURCHASES, purchase);
    const next = this._entries().filter((p) => p.volumeId !== purchase.volumeId);
    this._entries.set([...next, purchase]);
  }

  async remove(volumeId: string): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    await this.hydrate();
    const db = await openComiCalDb();
    if (!db) return;
    await db.delete(STORE_PURCHASES, volumeId);
    this._entries.set(this._entries().filter((p) => p.volumeId !== volumeId));
  }

  async clear(): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    const db = await openComiCalDb();
    if (!db) return;
    await db.clear(STORE_PURCHASES);
    this._entries.set([]);
  }

  /** @internal — used by the merge/import flow. */
  async _bulkPutInternal(records: readonly AnonymousPurchase[]): Promise<void> {
    if (!isIndexedDbAvailable() || records.length === 0) return;
    const db = await openComiCalDb();
    if (!db) return;
    const tx = db.transaction(STORE_PURCHASES, 'readwrite');
    await Promise.all(records.map((r) => tx.store.put(r)));
    await tx.done;
    const all = await db.getAll(STORE_PURCHASES);
    this._entries.set(all);
    this.hydrated = true;
  }

  private async hydrate(): Promise<void> {
    if (this.hydrated) return;
    if (this.hydrating) return this.hydrating;
    this.hydrating = (async () => {
      const db = await openComiCalDb();
      if (!db) {
        this.hydrated = true;
        return;
      }
      const all = await db.getAll(STORE_PURCHASES);
      this._entries.set(all);
      this.hydrated = true;
    })();
    return this.hydrating;
  }
}
