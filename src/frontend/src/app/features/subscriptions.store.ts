import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Subscription {
  subscriptionId: string;
  seriesId: string;
  seriesTitle: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionsStore {
  private readonly http = inject(HttpClient);

  readonly items = signal<Subscription[]>([]);
  readonly showSubscribedOnly = signal(false);
  readonly isLoading = signal(false);

  readonly subscribedSeriesIds = computed(() => new Set(this.items().map(s => s.seriesId)));

  isSubscribed(seriesId: string) {
    return computed(() => this.subscribedSeriesIds().has(seriesId));
  }

  load() {
    this.isLoading.set(true);
    this.http.get<{ items: Subscription[] }>('/api/v1/me/subscriptions').subscribe({
      next: r => { this.items.set(r.items); this.isLoading.set(false); },
      error: () => this.isLoading.set(false),
    });
  }

  subscribe(seriesId: string) {
    return this.http.post<Subscription>('/api/v1/me/subscriptions', { seriesId });
  }

  unsubscribe(seriesId: string) {
    return this.http.delete(`/api/v1/me/subscriptions/${seriesId}`);
  }

  toggleSubscribedOnly() {
    this.showSubscribedOnly.update(v => !v);
  }
}
