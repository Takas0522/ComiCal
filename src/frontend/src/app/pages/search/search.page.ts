import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

interface SeriesResult {
  seriesId: string;
  title: string;
  authors: { name: string; role: string }[];
  publisher: { name: string };
  isCompleted?: boolean;
  isSubscribed?: boolean;
}

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [SearchBarComponent, SpinnerComponent, RouterLink, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
    <div data-testid="page-search" class="py-5">
      <h1 class="text-xl font-bold mb-4" style="color: var(--color-text-primary)">検索</h1>
      <app-search-bar
        placeholder="タイトル・著者・出版社で検索..."
        [value]="query()"
        (search)="onSearch($event)"
        class="mb-5 block"
      />
      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (query() && results().length === 0) {
        <div class="text-center py-16">
          <p class="text-3xl mb-3" aria-hidden="true">🔍</p>
          <p style="color: var(--color-text-secondary)">「{{ query() }}」に一致するシリーズが見つかりませんでした。</p>
        </div>
      } @else if (!query()) {
        <div class="text-center py-16">
          <p class="text-4xl mb-3" aria-hidden="true">📚</p>
          <p style="color: var(--color-text-secondary)">キーワードを入力して検索してください。</p>
        </div>
      } @else {
        <ul class="flex flex-col gap-2">
          @for (series of results(); track series.seriesId) {
            <li
              class="flex items-center justify-between gap-4 p-4 rounded-xl"
              style="background: var(--color-surface); box-shadow: var(--shadow-card)"
            >
              <a [routerLink]="['/series', series.seriesId]" class="flex-1 min-w-0">
                <p class="font-semibold truncate" style="color: var(--color-text-primary)">{{ series.title }}</p>
                <p class="text-sm truncate mt-0.5" style="color: var(--color-text-secondary)">
                  {{ series.authors[0]?.name ?? '著者不明' }} &nbsp;/&nbsp; {{ series.publisher.name }}
                  @if (series.isCompleted) {
                    <span class="ml-2 text-xs px-1.5 py-0.5 rounded-full" style="background: var(--color-surface-elevated); color: var(--color-text-tertiary)">完結</span>
                  }
                </p>
              </a>
              <button
                type="button"
                class="shrink-0 px-4 py-1.5 text-sm font-semibold rounded-full transition-all"
                [style]="series.isSubscribed
                  ? 'background: var(--color-surface-elevated); color: var(--color-text-secondary); border: 1px solid var(--color-border)'
                  : 'background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); color: white; box-shadow: 0 2px 8px rgba(232,0,45,0.3)'"
                (click)="toggleSubscription(series)"
              >{{ series.isSubscribed ? '購読中' : '購読する' }}</button>
            </li>
          }
        </ul>
      }
    </div>
    </app-page-layout>
  `,
})
export class SearchPage {
  private readonly http = inject(HttpClient);
  private readonly subscriptionsStore = inject(SubscriptionsStore);

  protected readonly query = signal('');
  protected readonly isLoading = signal(false);
  protected readonly results = signal<SeriesResult[]>([]);

  onSearch(q: string) {
    const trimmed = q.trim();
    this.query.set(trimmed);
    if (!trimmed) { this.results.set([]); return; }
    this.isLoading.set(true);
    this.http.get<{ items: SeriesResult[] }>(`/api/v1/series?q=${encodeURIComponent(trimmed)}`)
      .subscribe({
        next: r => { this.results.set(r.items); this.isLoading.set(false); },
        error: () => { this.results.set([]); this.isLoading.set(false); },
      });
  }

  toggleSubscription(series: SeriesResult) {
    if (series.isSubscribed) {
      this.subscriptionsStore.unsubscribe(series.seriesId).subscribe({
        next: () => this.results.update(list =>
          list.map(s => s.seriesId === series.seriesId ? { ...s, isSubscribed: false } : s)),
      });
    } else {
      this.subscriptionsStore.subscribe(series.seriesId).subscribe({
        next: () => this.results.update(list =>
          list.map(s => s.seriesId === series.seriesId ? { ...s, isSubscribed: true } : s)),
      });
    }
  }
}
