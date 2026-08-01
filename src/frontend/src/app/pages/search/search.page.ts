import { afterNextRender, ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import {
  MAX_KEYWORDS,
  normalizeKeyword,
  UpcomingFilterStore,
} from '../../features/upcoming-filter.store';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

interface SeriesResult {
  seriesId: string;
  title: string;
  authors: { name: string; role: string }[] | null;
  publisher: { name: string } | null;
  isCompleted?: boolean;
  isSubscribed?: boolean;
}

interface RakutenCandidate {
  isbn: string;
  title: string;
  author: string | null;
  publisherName: string | null;
  thumbnailUrl: string | null;
  itemUrl: string | null;
}

interface SearchResponse {
  items: SeriesResult[];
  nextCursor: string | null;
  rakutenCandidates: RakutenCandidate[];
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
        @if (canRegisterKeyword()) {
          <div class="mb-5">
            <button
              type="button"
              data-testid="search-register-keyword"
              (click)="registerKeyword()"
              class="rounded-lg border border-[--color-primary] px-4 py-2 text-sm font-semibold text-[--color-primary] transition-colors hover:bg-[--color-primary-light]"
              i18n
            >
              「{{ query() }}」を絞り込みキーワードに登録
            </button>
          </div>
        }
        <p data-testid="search-keyword-status" aria-live="polite" class="sr-only">
          {{ keywordRegistrationStatus() }}
        </p>
        @if (isLoading()) {
          <div class="flex justify-center py-16"><app-spinner /></div>
        } @else if (query() && results().length === 0 && rakutenCandidates().length === 0) {
          <div class="text-center py-16">
            <p class="text-3xl mb-3" aria-hidden="true">🔍</p>
            <p style="color: var(--color-text-secondary)">
              「{{ query() }}」に一致するシリーズが見つかりませんでした。
            </p>
          </div>
        } @else if (!query()) {
          <div class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">📚</p>
            <p style="color: var(--color-text-secondary)">キーワードを入力して検索してください。</p>
          </div>
        } @else {
          @if (results().length > 0) {
            <ul class="flex flex-col gap-2">
              @for (series of results(); track series.seriesId) {
                <li
                  class="flex items-center justify-between gap-4 p-4 rounded-xl"
                  style="background: var(--color-surface); box-shadow: var(--shadow-card)"
                >
                  <a [routerLink]="['/series', series.seriesId]" class="flex-1 min-w-0">
                    <p class="font-semibold truncate" style="color: var(--color-text-primary)">
                      {{ series.title }}
                    </p>
                    <p class="text-sm truncate mt-0.5" style="color: var(--color-text-secondary)">
                      {{ series.authors?.[0]?.name ?? '著者不明' }} &nbsp;/&nbsp;
                      {{ series.publisher?.name ?? '出版社不明' }}
                      @if (series.isCompleted) {
                        <span
                          class="ml-2 text-xs px-1.5 py-0.5 rounded-full"
                          style="background: var(--color-surface-elevated); color: var(--color-text-tertiary)"
                          >完結</span
                        >
                      }
                    </p>
                  </a>
                  <button
                    type="button"
                    data-testid="subscribe-button"
                    class="shrink-0 px-4 py-1.5 text-sm font-semibold rounded-full transition-all"
                    [style]="
                      isSubscribed(series.seriesId)
                        ? 'background: var(--color-surface-elevated); color: var(--color-text-secondary); border: 1px solid var(--color-border)'
                        : 'background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); color: white; box-shadow: 0 2px 8px rgba(232,0,45,0.3)'
                    "
                    (click)="toggleSubscription(series)"
                  >
                    {{ isSubscribed(series.seriesId) ? '購読中' : '購読する' }}
                  </button>
                </li>
              }
            </ul>
          }

          @if (rakutenCandidates().length > 0) {
            <div class="mt-6">
              <p class="text-sm font-semibold mb-2" style="color: var(--color-text-secondary)">
                楽天 Books の候補（未登録）
              </p>
              <ul class="flex flex-col gap-2">
                @for (candidate of rakutenCandidates(); track candidate.isbn) {
                  <li
                    class="flex items-center justify-between gap-4 p-4 rounded-xl"
                    style="background: var(--color-surface); box-shadow: var(--shadow-card)"
                  >
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2">
                        <p class="font-semibold truncate" style="color: var(--color-text-primary)">
                          {{ candidate.title }}
                        </p>
                        <span
                          class="shrink-0 text-xs px-1.5 py-0.5 rounded-full"
                          style="background: #fff3e0; color: #e65100"
                          aria-label="楽天候補"
                          >楽天</span
                        >
                      </div>
                      <p class="text-sm truncate mt-0.5" style="color: var(--color-text-secondary)">
                        {{ candidate.author ?? '著者不明' }} &nbsp;/&nbsp;
                        {{ candidate.publisherName ?? '出版社不明' }}
                      </p>
                    </div>
                    <button
                      type="button"
                      data-testid="subscribe-rakuten-button"
                      class="shrink-0 px-4 py-1.5 text-sm font-semibold rounded-full transition-all"
                      [disabled]="subscribingIsbn() === candidate.isbn"
                      style="background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); color: white; box-shadow: 0 2px 8px rgba(232,0,45,0.3)"
                      (click)="subscribeFromRakuten(candidate)"
                    >
                      @if (subscribingIsbn() === candidate.isbn) {
                        <span aria-label="処理中">...</span>
                      } @else {
                        購読する
                      }
                    </button>
                  </li>
                }
              </ul>
            </div>
          }
        }

        @if (subscribeError()) {
          <div
            role="alert"
            class="mt-4 p-3 rounded-lg text-sm"
            style="background: #ffeaea; color: #c62828"
          >
            {{ subscribeError() }}
          </div>
        }
      </div>
    </app-page-layout>
  `,
})
export class SearchPage {
  private readonly http = inject(HttpClient);
  private readonly subscriptionsStore = inject(SubscriptionsStore);
  private readonly upcomingFilterStore = inject(UpcomingFilterStore);

  protected readonly query = signal('');
  protected readonly isLoading = signal(false);
  protected readonly results = signal<SeriesResult[]>([]);
  protected readonly rakutenCandidates = signal<RakutenCandidate[]>([]);
  protected readonly subscribingIsbn = signal<string | null>(null);
  protected readonly subscribeError = signal<string | null>(null);
  protected readonly keywordRegistrationStatus = signal('');

  protected canRegisterKeyword(): boolean {
    const query = this.query();
    const normalizedQuery = normalizeKeyword(query);
    return (
      normalizedQuery.length > 0 &&
      !this.upcomingFilterStore.keywords().some((keyword) => keyword === normalizedQuery)
    );
  }

  constructor() {
    afterNextRender(() => void this.upcomingFilterStore.restore());
  }

  onSearch(q: string) {
    const trimmed = q.trim();
    this.query.set(trimmed);
    if (!trimmed) {
      this.results.set([]);
      this.rakutenCandidates.set([]);
      return;
    }

    this.isLoading.set(true);
    this.subscribeError.set(null);
    this.http.get<SearchResponse>(`/api/v1/series?q=${encodeURIComponent(trimmed)}`).subscribe({
      next: (r) => {
        this.results.set(r.items);
        this.rakutenCandidates.set(r.rakutenCandidates ?? []);
        this.isLoading.set(false);
      },
      error: () => {
        this.results.set([]);
        this.rakutenCandidates.set([]);
        this.isLoading.set(false);
      },
    });
  }

  async registerKeyword() {
    const result = await this.upcomingFilterStore.addKeyword(this.query());
    if (result.success) {
      this.keywordRegistrationStatus.set('絞り込みキーワードに登録しました。');
      return;
    }

    if (result.reason === 'too-long') {
      this.keywordRegistrationStatus.set('キーワードの合計は512文字以内にしてください。');
    } else if (result.reason === 'too-many-keywords') {
      this.keywordRegistrationStatus.set(`キーワードは${MAX_KEYWORDS}件まで登録できます。`);
    } else if (result.reason === 'duplicate') {
      this.keywordRegistrationStatus.set('同じキーワードは登録できません。');
    } else {
      this.keywordRegistrationStatus.set('絞り込みキーワードを登録できませんでした。');
    }
  }

  toggleSubscription(series: SeriesResult) {
    if (this.isSubscribed(series.seriesId)) {
      this.subscriptionsStore.unsubscribe(series.seriesId).subscribe();
    } else {
      this.subscriptionsStore.subscribe(series.seriesId, series.title).subscribe();
    }
  }

  subscribeFromRakuten(candidate: RakutenCandidate) {
    if (this.subscribingIsbn() === candidate.isbn) return;

    this.subscribingIsbn.set(candidate.isbn);
    this.subscribeError.set(null);
    this.subscriptionsStore.subscribeFromRakuten(candidate.isbn).subscribe({
      next: () => {
        this.subscribingIsbn.set(null);
        // 購読済みになった候補をリストから除外
        this.rakutenCandidates.update((list) => list.filter((c) => c.isbn !== candidate.isbn));
      },
      error: (err) => {
        this.subscribingIsbn.set(null);
        const status = err?.status;
        if (status === 404) {
          this.subscribeError.set('楽天 Books で該当タイトルが見つかりませんでした。');
        } else if (status === 429) {
          this.subscribeError.set('リクエストが多すぎます。しばらくしてから再試行してください。');
        } else if (status === 409) {
          this.subscribeError.set('すでに購読済みです。');
        } else {
          this.subscribeError.set('購読登録に失敗しました。');
        }
      },
    });
  }

  isSubscribed(seriesId: string): boolean {
    return this.subscriptionsStore.subscribedSeriesIds().has(seriesId);
  }
}
