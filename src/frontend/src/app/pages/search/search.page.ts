import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import {
  TabListComponent,
  type TabItem,
} from '../../molecules/tab-list/tab-list.component';
import { SeriesSearchResultsComponent } from '../../organisms/series-search-results/series-search-results.component';
import { VolumeSearchResultsComponent } from '../../organisms/volume-search-results/volume-search-results.component';
import { PaginationCursorComponent } from '../../molecules/pagination-cursor/pagination-cursor.component';
import { SeriesApi } from '../../core/api/series.api';
import { VolumeApi } from '../../core/api/volume.api';
import type {
  PagedResult,
  SeriesSummary,
  Volume,
} from '../../core/api/api-types';

type SearchTab = 'series' | 'volumes';
const TABS: readonly TabItem<SearchTab>[] = [
  { id: 'series', label: 'シリーズ', testid: 'tab-series' },
  { id: 'volumes', label: '巻', testid: 'tab-volumes' },
];

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [
    PageLayoutComponent,
    SearchBarComponent,
    TabListComponent,
    SeriesSearchResultsComponent,
    VolumeSearchResultsComponent,
    PaginationCursorComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="検索" testid="search">
      <app-search-bar
        [initialValue]="q() ?? ''"
        (searchTerm)="onSearch($event)"
      />
      <app-tab-list [items]="tabs" [active]="tab()" (tabChange)="onTabChange($event)" />

      @if (tab() === 'series') {
        <app-series-search-results [items]="seriesItems()" [loading]="loading()" />
        <app-pagination-cursor
          [nextCursor]="seriesCursor()"
          [loading]="loading()"
          (loadMore)="loadMoreSeries()"
        />
      } @else {
        <app-volume-search-results [items]="volumeItems()" [loading]="loading()" />
        <app-pagination-cursor
          [nextCursor]="volumeCursor()"
          [loading]="loading()"
          (loadMore)="loadMoreVolumes()"
        />
      }
    </app-page-layout>
  `,
})
export class SearchPage {
  private readonly seriesApi = inject(SeriesApi);
  private readonly volumeApi = inject(VolumeApi);
  private readonly router = inject(Router);

  /** Bound from the route via withComponentInputBinding. */
  readonly q = input<string | undefined>(undefined);
  readonly tab = input<SearchTab>('series');

  protected readonly tabs = TABS;

  protected readonly seriesItems = signal<readonly SeriesSummary[]>([]);
  protected readonly seriesCursor = signal<string | null | undefined>(null);
  protected readonly volumeItems = signal<readonly Volume[]>([]);
  protected readonly volumeCursor = signal<string | null | undefined>(null);
  protected readonly loading = signal<boolean>(false);

  protected readonly hasQuery = computed(() => (this.q() ?? '').trim().length > 0);

  constructor() {
    // Re-fetch whenever q or tab changes (route-bound input → signal).
    effect(() => {
      const term = (this.q() ?? '').trim();
      const t = this.tab();
      if (!term) {
        this.seriesItems.set([]);
        this.seriesCursor.set(null);
        this.volumeItems.set([]);
        this.volumeCursor.set(null);
        this.loading.set(false);
        return;
      }
      this.loading.set(true);
      if (t === 'series') {
        this.seriesApi.searchSeries({ q: term, limit: 20 }).subscribe({
          next: (r: PagedResult<SeriesSummary>) => {
            this.seriesItems.set(r.items);
            this.seriesCursor.set(r.nextCursor ?? null);
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      } else {
        this.volumeApi.searchVolumes({ q: term, limit: 24 }).subscribe({
          next: (r: PagedResult<Volume>) => {
            this.volumeItems.set(r.items);
            this.volumeCursor.set(r.nextCursor ?? null);
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      }
    });
  }

  protected onSearch(term: string): void {
    void this.router.navigate(['/search'], {
      queryParams: { q: term, tab: this.tab() },
      queryParamsHandling: 'merge',
    });
  }

  protected onTabChange(t: string): void {
    void this.router.navigate(['/search'], {
      queryParams: { tab: t as SearchTab },
      queryParamsHandling: 'merge',
    });
  }

  protected loadMoreSeries(): void {
    const cursor = this.seriesCursor();
    const term = (this.q() ?? '').trim();
    if (!cursor || !term) return;
    this.loading.set(true);
    this.seriesApi.searchSeries({ q: term, limit: 20, cursor }).subscribe({
      next: (r) => {
        this.seriesItems.update((arr) => [...arr, ...r.items]);
        this.seriesCursor.set(r.nextCursor ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadMoreVolumes(): void {
    const cursor = this.volumeCursor();
    const term = (this.q() ?? '').trim();
    if (!cursor || !term) return;
    this.loading.set(true);
    this.volumeApi.searchVolumes({ q: term, limit: 24, cursor }).subscribe({
      next: (r) => {
        this.volumeItems.update((arr) => [...arr, ...r.items]);
        this.volumeCursor.set(r.nextCursor ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
