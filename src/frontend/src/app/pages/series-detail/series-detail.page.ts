import { Component, ChangeDetectionStrategy, input, signal, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReleaseDatePipe } from '../../shared/pipes/release-date.pipe';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

interface Volume {
  volumeId: string;
  volumeNumber?: number | null;
  isbn13: string;
  releaseDate: string | null;
  releaseDateIsMonthOnly: boolean;
  thumbnailUrl?: string | null;
  rakutenItemUrl?: string | null;
}

interface Series {
  seriesId: string;
  title: string;
  authors: { name: string; role: string }[];
  publisher: { name: string };
  isCompleted?: boolean;
  isSubscribed?: boolean;
  volumes?: Volume[];
}

@Component({
  selector: 'app-series-detail-page',
  standalone: true,
  imports: [ReleaseDatePipe, SpinnerComponent, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
    <div data-testid="page-series-detail" class="py-5">
      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (series()) {
        <div class="mb-5">
          <h1 class="text-xl font-bold mb-1" style="color: var(--color-text-primary)">{{ series()!.title }}</h1>
          <p class="text-sm" style="color: var(--color-text-secondary)">
            {{ series()!.authors[0]?.name ?? '' }} &nbsp;/&nbsp; {{ series()!.publisher.name }}
            @if (series()!.isCompleted) {
              <span class="ml-2 text-xs px-1.5 py-0.5 rounded-full" style="background: var(--color-surface-elevated); color: var(--color-text-tertiary)">完結</span>
            }
          </p>
        </div>
        <button
          type="button"
          class="mb-5 px-5 py-2 rounded-full text-sm font-semibold transition-all"
          [style]="isSubscribed()
            ? 'background: var(--color-surface-elevated); color: var(--color-text-secondary); border: 1px solid var(--color-border)'
            : 'background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); color: white; box-shadow: 0 2px 8px rgba(232,0,45,0.3)'"
          (click)="toggleSubscription()"
        >{{ isSubscribed() ? '購読解除' : '購読する' }}</button>

        <h2 class="text-sm font-semibold mb-3" style="color: var(--color-text-secondary)">巻一覧</h2>
        @if (!series()!.volumes?.length) {
          <p style="color: var(--color-text-secondary)">巻情報がありません。</p>
        } @else {
          <ul class="flex flex-col gap-2">
            @for (vol of series()!.volumes; track vol.volumeId) {
              <li class="px-4 py-3 flex items-center gap-3 rounded-xl" style="background: var(--color-surface); box-shadow: var(--shadow-card)">
                @if (vol.thumbnailUrl) {
                  <img [src]="vol.thumbnailUrl" [alt]="series()!.title + ' 第' + (vol.volumeNumber ?? '?') + '巻'"
                       class="w-10 h-14 object-cover rounded-lg shrink-0" loading="lazy" />
                }
                <div class="flex-1 min-w-0">
                  <p class="font-semibold" style="color: var(--color-text-primary)">
                    @if (vol.volumeNumber) { 第{{ vol.volumeNumber }}巻 } @else { 単巻 }
                  </p>
                  <p class="text-sm" style="color: var(--color-text-secondary)">
                    {{ vol.releaseDate | releaseDate:vol.releaseDateIsMonthOnly }}
                  </p>
                </div>
                @if (vol.rakutenItemUrl) {
                  <a [href]="vol.rakutenItemUrl" target="_blank" rel="noopener noreferrer"
                     class="shrink-0 text-xs font-medium" style="color: var(--color-primary)">楽天で見る →</a>
                }
              </li>
            }
          </ul>
        }
      } @else {
        <p class="py-16 text-center" style="color: var(--color-text-secondary)">シリーズが見つかりませんでした。</p>
      }
    </div>
  `,
})
export class SeriesDetailPage implements OnInit {
  readonly id = input('');

  private readonly http = inject(HttpClient);
  private readonly subscriptionsStore = inject(SubscriptionsStore);

  protected readonly series = signal<Series | null>(null);
  protected readonly isLoading = signal(false);

  ngOnInit() {
    const seriesId = this.id();
    if (!seriesId) return;
    this.isLoading.set(true);
    this.http.get<Series>(`/api/v1/series/${seriesId}`).subscribe({
      next: s => { this.series.set(s); this.isLoading.set(false); },
      error: () => { this.series.set(null); this.isLoading.set(false); },
    });
  }

  toggleSubscription() {
    const s = this.series();
    if (!s) return;
    if (this.isSubscribed()) {
      this.subscriptionsStore.unsubscribe(s.seriesId).subscribe();
    } else {
      this.subscriptionsStore.subscribe(s.seriesId, s.title).subscribe();
    }
  }

  isSubscribed(): boolean {
    const s = this.series();
    return s ? this.subscriptionsStore.subscribedSeriesIds().has(s.seriesId) : false;
  }
}
