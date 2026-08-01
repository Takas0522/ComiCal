import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  OnInit,
} from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { CardGridComponent } from '../../organisms/card-grid/card-grid.component';
import { Volume } from '../../molecules/volume-card/volume-card.component';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CardGridComponent, PageLayoutComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-home" class="py-5">
        <div class="flex items-center justify-between mb-5 gap-3 flex-wrap">
          <h1 class="text-xl font-bold" style="color: var(--color-text-primary)">直近の発売予定</h1>
          <div class="flex items-center gap-4 flex-wrap">
            <a
              data-testid="home-keywords-settings-link"
              routerLink="/settings/keywords"
              class="text-sm font-semibold"
              style="color: var(--color-primary)"
              i18n
            >
              絞り込みを設定
            </a>
            <label
              class="inline-flex items-center gap-2 text-sm cursor-pointer select-none"
              style="color: var(--color-text-secondary)"
            >
              <input
                type="checkbox"
                data-testid="filter-subscribed-only"
                class="w-4 h-4 accent-current"
                [checked]="subscribedOnly()"
                (change)="toggleSubscribedOnly()"
              />
              購読中のみ
            </label>
          </div>
        </div>
        @if (filterStore.restored() && filterStore.keywords().length > 0) {
          <div
            data-testid="home-active-keywords"
            class="mb-5 flex items-center gap-2 flex-wrap"
            aria-label="適用中の絞り込みキーワード"
            i18n-aria-label
          >
            @for (keyword of filterStore.keywords(); track keyword) {
              <span
                data-testid="home-active-keyword-chip"
                class="rounded-full px-3 py-1 text-sm"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)"
              >
                {{ keyword }}
              </span>
            }
          </div>
        }
        @if (subscribedOnly() && subscribedCount() === 0 && !isLoading()) {
          <div class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">⭐</p>
            <p style="color: var(--color-text-secondary)">購読中のシリーズはまだありません。</p>
            <a
              routerLink="/search"
              class="inline-block mt-4 px-5 py-2 rounded-full text-sm font-semibold text-white btn-primary"
            >
              検索して追加する</a
            >
          </div>
        } @else if (
          filterStore.restored() &&
          filterStore.keywords().length > 0 &&
          filteredVolumes().length === 0 &&
          !isLoading()
        ) {
          <div data-testid="home-keyword-empty-state" class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">📚</p>
            <p style="color: var(--color-text-secondary)" i18n>
              指定したキーワードに一致する発売予定はありません。
            </p>
          </div>
        } @else {
          <app-card-grid [volumes]="filteredVolumes()" [loading]="isLoading()" />
        }
      </div>
    </app-page-layout>
  `,
})
export class HomePage implements OnInit {
  private static readonly STORAGE_KEY = 'home_subscribed_only';

  private readonly http = inject(HttpClient);
  private readonly subscriptions = inject(SubscriptionsStore);
  protected readonly filterStore = inject(UpcomingFilterStore);

  protected readonly volumes = signal<Volume[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly subscribedOnly = signal(this.readInitialFilter());

  protected readonly subscribedCount = computed(
    () => this.subscriptions.subscribedSeriesIds().size,
  );

  protected readonly filteredVolumes = computed(() => {
    if (!this.subscribedOnly()) return this.volumes();
    const ids = this.subscriptions.subscribedSeriesIds();
    return this.volumes().filter((v) => ids.has(v.seriesId));
  });

  async ngOnInit(): Promise<void> {
    await this.filterStore.restore();
    this.fetch();
  }

  private fetch() {
    this.isLoading.set(true);
    const params = new HttpParams().set('q', JSON.stringify(this.filterStore.keywords()));
    this.http.get<{ items: any[] }>('/api/v1/volumes/upcoming', { params }).subscribe({
      next: (r) => {
        this.volumes.set(
          r.items.map((v) => ({
            id: v.volumeId,
            title: v.series?.title ?? '不明',
            isbn: v.isbn13,
            releaseDate: v.releaseDate,
            releaseDateIsMonthOnly: v.releaseDateIsMonthOnly,
            thumbnailUrl: v.thumbnailUrl ?? null,
            seriesId: v.series?.seriesId ?? '',
            seriesTitle: v.series?.title ?? '',
            volumeNumber: v.volumeNumber ?? 0,
            rakutenItemUrl: v.rakutenItemUrl ?? null,
          })),
        );
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  toggleSubscribedOnly() {
    const next = !this.subscribedOnly();
    this.subscribedOnly.set(next);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(HomePage.STORAGE_KEY, next ? '1' : '0');
    }
  }

  private readInitialFilter(): boolean {
    if (typeof localStorage === 'undefined') return true;
    const saved = localStorage.getItem(HomePage.STORAGE_KEY);
    if (saved === null) return true; // default ON
    return saved === '1';
  }
}
