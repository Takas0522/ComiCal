import { Injectable, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface AnonymousSubscription {
  seriesId: string;
  seriesTitle: string;
  createdAt: string;
}

// Stores anonymous (logged-out) user data in localStorage.
// Spec: F-AUTH-01 匿名利用 — 未ログイン状態でも全機能を利用可能。
@Injectable({ providedIn: 'root' })
export class AnonymousStore {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly STORAGE_KEY = 'anon_subscriptions';

  readonly subscriptions = signal<AnonymousSubscription[]>([]);

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (!saved) return;
    try {
      const parsed = JSON.parse(saved) as unknown;
      if (Array.isArray(parsed)) {
        // Migrate legacy format (string[]) to AnonymousSubscription[]
        const items: AnonymousSubscription[] = parsed.map((v) =>
          typeof v === 'string'
            ? { seriesId: v, seriesTitle: '', createdAt: new Date(0).toISOString() }
            : (v as AnonymousSubscription),
        );
        this.subscriptions.set(items);
      }
    } catch {
      /* ignore malformed */
    }
  }

  has(seriesId: string): boolean {
    return this.subscriptions().some((s) => s.seriesId === seriesId);
  }

  add(seriesId: string, seriesTitle: string): AnonymousSubscription {
    const existing = this.subscriptions().find((s) => s.seriesId === seriesId);
    if (existing) return existing;
    const item: AnonymousSubscription = {
      seriesId,
      seriesTitle,
      createdAt: new Date().toISOString(),
    };
    this.subscriptions.update((list) => {
      const updated = [...list, item];
      this.persist(updated);
      return updated;
    });
    return item;
  }

  remove(seriesId: string) {
    this.subscriptions.update((list) => {
      const updated = list.filter((s) => s.seriesId !== seriesId);
      this.persist(updated);
      return updated;
    });
  }

  clear() {
    this.subscriptions.set([]);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.STORAGE_KEY);
    }
  }

  private persist(items: AnonymousSubscription[]) {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(items));
  }
}
