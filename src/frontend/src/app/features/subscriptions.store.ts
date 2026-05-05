import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
import { AnonymousStore, AnonymousSubscription } from './anonymous.store';
import { AuthStore } from './auth.store';

export interface Subscription {
  subscriptionId: string;
  seriesId: string;
  seriesTitle: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionsStore {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthStore);
  private readonly anon = inject(AnonymousStore);

  readonly items = signal<Subscription[]>([]);
  readonly showSubscribedOnly = signal(false);
  readonly isLoading = signal(false);

  readonly subscribedSeriesIds = computed(() => new Set(this.items().map((s) => s.seriesId)));

  isSubscribed(seriesId: string) {
    return computed(() => this.subscribedSeriesIds().has(seriesId));
  }

  load() {
    if (!this.auth.isLoggedIn()) {
      this.items.set(this.anon.subscriptions().map(toSubscription));
      this.isLoading.set(false);
      return;
    }
    this.isLoading.set(true);
    this.http.get<{ items: Subscription[] }>('/api/v1/me/subscriptions').subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  subscribe(seriesId: string, seriesTitle = ''): Observable<Subscription> {
    if (!this.auth.isLoggedIn()) {
      const added = this.anon.add(seriesId, seriesTitle);
      const sub = toSubscription(added);
      this.items.update((list) =>
        list.some((s) => s.seriesId === seriesId) ? list : [...list, sub],
      );
      return of(sub);
    }
    return this.http
      .post<Subscription>('/api/v1/me/subscriptions', { seriesId })
      .pipe(tap((sub) => this.items.update((list) => [...list, sub])));
  }

  /**
   * 楽天候補（DB未登録）を ISBN で購読登録します。
   * バックエンドが Series を UPSERT してから購読を作成します。
   */
  subscribeFromRakuten(rakutenIsbn: string): Observable<Subscription> {
    if (!this.auth.isLoggedIn()) {
      // 匿名ユーザーには ISBN ベースの購読は未サポート（ログインを促す）
      return new Observable((obs) => {
        obs.error({ status: 401 });
      });
    }
    return this.http
      .post<Subscription>('/api/v1/me/subscriptions', { rakutenIsbn })
      .pipe(tap((sub) => this.items.update((list) => [...list, sub])));
  }

  unsubscribe(seriesId: string): Observable<unknown> {
    if (!this.auth.isLoggedIn()) {
      this.anon.remove(seriesId);
      this.items.update((list) => list.filter((s) => s.seriesId !== seriesId));
      return of(void 0);
    }
    return this.http
      .delete(`/api/v1/me/subscriptions/${seriesId}`)
      .pipe(tap(() => this.items.update((list) => list.filter((s) => s.seriesId !== seriesId))));
  }

  toggleSubscribedOnly() {
    this.showSubscribedOnly.update((v) => !v);
  }
}

function toSubscription(a: AnonymousSubscription): Subscription {
  return {
    subscriptionId: `anon-${a.seriesId}`,
    seriesId: a.seriesId,
    seriesTitle: a.seriesTitle,
    createdAt: a.createdAt,
  };
}
