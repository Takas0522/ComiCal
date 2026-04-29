import { Injectable, inject } from '@angular/core';

import { AnonymousPurchaseRepository } from './anonymous-purchase.repository';
import { AnonymousSubscriptionRepository } from './anonymous-subscription.repository';
import {
  ANONYMOUS_EXPORT_SCHEMA_VERSION,
  type AnonymousExport,
  type AnonymousPurchase,
  type AnonymousSubscription,
} from './types';

export class AnonymousImportSchemaError extends Error {
  constructor(public readonly received: unknown) {
    super(
      `Unsupported anonymous export schemaVersion: ${String(received)} (expected ${ANONYMOUS_EXPORT_SCHEMA_VERSION})`,
    );
    this.name = 'AnonymousImportSchemaError';
  }
}

/**
 * Export / import (and merge) for the anonymous IndexedDB store.
 * Used by the post-login merge flow and the QR-sync feature.
 */
@Injectable({ providedIn: 'root' })
export class AnonymousStoreExportService {
  private readonly subscriptions = inject(AnonymousSubscriptionRepository);
  private readonly purchases = inject(AnonymousPurchaseRepository);

  async exportAll(): Promise<AnonymousExport> {
    const [subs, purs] = await Promise.all([
      this.subscriptions.list(),
      this.purchases.list(),
    ]);
    return {
      schemaVersion: ANONYMOUS_EXPORT_SCHEMA_VERSION,
      exportedAt: new Date().toISOString(),
      subscriptions: [...subs],
      purchases: [...purs],
    };
  }

  /**
   * Merge an exported payload into the local store. Conflict resolution is
   * **last-write-wins by `updatedAt`** — the incoming record only overrides
   * an existing one if its `updatedAt` is strictly newer.
   */
  async importAll(payload: AnonymousExport): Promise<void> {
    if (payload.schemaVersion !== ANONYMOUS_EXPORT_SCHEMA_VERSION) {
      throw new AnonymousImportSchemaError(payload.schemaVersion);
    }

    const existingSubs = await this.subscriptions.list();
    const subsBySeries = new Map(existingSubs.map((s) => [s.seriesId, s]));
    const subsToWrite: AnonymousSubscription[] = [];
    for (const incoming of payload.subscriptions) {
      const cur = subsBySeries.get(incoming.seriesId);
      if (!cur || incoming.updatedAt > cur.updatedAt) {
        subsToWrite.push(incoming);
      }
    }
    await this.subscriptions._bulkPutInternal(subsToWrite);

    const existingPurs = await this.purchases.list();
    const pursByVolume = new Map(existingPurs.map((p) => [p.volumeId, p]));
    const pursToWrite: AnonymousPurchase[] = [];
    for (const incoming of payload.purchases) {
      const cur = pursByVolume.get(incoming.volumeId);
      if (!cur || incoming.updatedAt > cur.updatedAt) {
        pursToWrite.push(incoming);
      }
    }
    await this.purchases._bulkPutInternal(pursToWrite);
  }
}
