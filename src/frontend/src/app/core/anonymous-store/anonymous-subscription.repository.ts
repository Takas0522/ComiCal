import { Injectable, computed, signal, type Signal } from '@angular/core';

import {
  STORE_SUBSCRIPTIONS,
  isIndexedDbAvailable,
  openComiCalDb,
} from './db';
import type { AnonymousSubscription } from './types';

/**
 * Repository for anonymous (pre-login) "want-to-read" subscriptions.
 *
 * - Backed by IndexedDB (`comical-anon` / `subscriptions` store).
 * - Maintains a signal cache so UI re-renders without a re-query.
 * - SSR-safe: when IndexedDB is unavailable, all reads return empty and
 *   writes are silent no-ops.
 */
@Injectable({ providedIn: 'root' })
export class AnonymousSubscriptionRepository {
  private readonly _entries = signal<readonly AnonymousSubscription[]>([]);
  private hydrated = false;
  private hydrating: Promise<void> | null = null;

  readonly entries: Signal<readonly AnonymousSubscription[]> =
    this._entries.asReadonly();

  readonly count: Signal<number> = computed(() => this._entries().length);

  constructor() {
    void this.hydrate();
  }

  /**
   * Reactive predicate for "is series subscribed?". Tracks `entries`
   * automatically so callers can use it inside `computed()` / templates.
   */
  hasSignal(seriesId: string): Signal<boolean> {
    return computed(() => this._entries().some((s) => s.seriesId === seriesId));
  }

  async list(): Promise<readonly AnonymousSubscription[]> {
    await this.hydrate();
    return this._entries();
  }

  async has(seriesId: string): Promise<boolean> {
    await this.hydrate();
    return this._entries().some((s) => s.seriesId === seriesId);
  }

  async add(seriesId: string, notes?: string): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    await this.hydrate();
    const db = await openComiCalDb();
    if (!db) return;
    const existing = this._entries().find((s) => s.seriesId === seriesId);
    const now = new Date().toISOString();
    const record: AnonymousSubscription = {
      seriesId,
      addedAt: existing?.addedAt ?? now,
      updatedAt: now,
      ...(notes !== undefined
        ? { notes }
        : existing?.notes !== undefined
          ? { notes: existing.notes }
          : {}),
    };
    await db.put(STORE_SUBSCRIPTIONS, record);
    const next = this._entries().filter((s) => s.seriesId !== seriesId);
    this._entries.set([...next, record]);
  }

  async remove(seriesId: string): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    await this.hydrate();
    const db = await openComiCalDb();
    if (!db) return;
    await db.delete(STORE_SUBSCRIPTIONS, seriesId);
    this._entries.set(this._entries().filter((s) => s.seriesId !== seriesId));
  }

  async clear(): Promise<void> {
    if (!isIndexedDbAvailable()) return;
    const db = await openComiCalDb();
    if (!db) return;
    await db.clear(STORE_SUBSCRIPTIONS);
    this._entries.set([]);
  }

  /** @internal — used by the merge/import flow. */
  async _bulkPutInternal(records: readonly AnonymousSubscription[]): Promise<void> {
    if (!isIndexedDbAvailable() || records.length === 0) return;
    const db = await openComiCalDb();
    if (!db) return;
    const tx = db.transaction(STORE_SUBSCRIPTIONS, 'readwrite');
    await Promise.all(records.map((r) => tx.store.put(r)));
    await tx.done;
    const all = await db.getAll(STORE_SUBSCRIPTIONS);
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
      const all = await db.getAll(STORE_SUBSCRIPTIONS);
      this._entries.set(all);
      this.hydrated = true;
    })();
    return this.hydrating;
  }
}
