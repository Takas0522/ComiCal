import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';

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
  imports: [SearchBarComponent, SpinnerComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-search" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">検索</h1>
      <app-search-bar
        placeholder="タイトル・著者・出版社で検索..."
        [value]="query()"
        (search)="onSearch($event)"
        class="mb-6 block"
      />
      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (query() && results().length === 0) {
        <p class="text-[--color-text-secondary] text-center py-16">
          「{{ query() }}」に一致するシリーズが見つかりませんでした。
        </p>
      } @else if (!query()) {
        <p class="text-[--color-text-secondary] text-center py-16">
          キーワードを入力して検索してください。
        </p>
      } @else {
        <ul class="divide-y divide-[--color-border]">
          @for (series of results(); track series.seriesId) {
            <li class="py-4 flex items-center justify-between gap-4">
              <a [routerLink]="['/series', series.seriesId]" class="flex-1 min-w-0">
                <p class="font-semibold text-[--color-text-primary] truncate">{{ series.title }}</p>
                <p class="text-sm text-[--color-text-secondary] truncate">
                  {{ series.authors[0]?.name ?? '著者不明' }} &nbsp;/&nbsp; {{ series.publisher.name }}
                  @if (series.isCompleted) { <span class="ml-2 text-xs text-[--color-text-secondary]">完結</span> }
                </p>
              </a>
              <button
                type="button"
                class="shrink-0 px-3 py-1.5 text-sm rounded-lg transition-colors"
                [class]="series.isSubscribed
                  ? 'bg-[--color-surface-elevated] text-[--color-text-secondary] border border-[--color-border] hover:bg-red-50 hover:text-red-600'
                  : 'bg-[--color-primary] text-white hover:bg-[--color-primary-hover]'"
                (click)="toggleSubscription(series)"
              >{{ series.isSubscribed ? '購読中' : '購読する' }}</button>
            </li>
          }
        </ul>
      }
    </div>
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
