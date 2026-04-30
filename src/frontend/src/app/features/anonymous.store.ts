import { Injectable, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

// Uses localStorage as a simple alternative to idb-keyval (no additional dependency)
@Injectable({ providedIn: 'root' })
export class AnonymousStore {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly STORAGE_KEY = 'anon_subscriptions';

  readonly localSeriesIds = signal<string[]>([]);

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved) {
      try { this.localSeriesIds.set(JSON.parse(saved)); } catch { /* ignore */ }
    }
  }

  addSeries(seriesId: string) {
    this.localSeriesIds.update(ids => {
      if (ids.includes(seriesId)) return ids;
      const updated = [...ids, seriesId];
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updated));
      }
      return updated;
    });
  }

  removeSeries(seriesId: string) {
    this.localSeriesIds.update(ids => {
      const updated = ids.filter(id => id !== seriesId);
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(updated));
      }
      return updated;
    });
  }

  clear() {
    this.localSeriesIds.set([]);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.STORAGE_KEY);
    }
  }
}
