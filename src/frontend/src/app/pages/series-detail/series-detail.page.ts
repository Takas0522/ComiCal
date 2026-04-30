import { Component, ChangeDetectionStrategy, input, signal, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReleaseDatePipe } from '../../shared/pipes/release-date.pipe';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';

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
  imports: [ReleaseDatePipe, SpinnerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-series-detail" class="py-6">
      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (series()) {
        <div class="mb-6">
          <h1 class="text-2xl font-bold text-[--color-text-primary] mb-1">{{ series()!.title }}</h1>
          <p class="text-sm text-[--color-text-secondary]">
            {{ series()!.authors[0]?.name ?? '' }} &nbsp;/&nbsp; {{ series()!.publisher.name }}
            @if (series()!.isCompleted) { <span class="ml-2">完結</span> }
          </p>
        </div>
        <button
          type="button"
          class="mb-6 px-4 py-2 rounded-lg text-sm font-semibold transition-colors"
          [class]="series()!.isSubscribed
            ? 'bg-[--color-surface-elevated] text-[--color-text-secondary] border border-[--color-border] hover:bg-red-50 hover:text-red-600'
            : 'bg-[--color-primary] text-white hover:bg-[--color-primary-hover]'"
          (click)="toggleSubscription()"
        >{{ series()!.isSubscribed ? '購読解除' : '購読する' }}</button>

        <h2 class="text-lg font-semibold text-[--color-text-primary] mb-3">巻一覧</h2>
        @if (!series()!.volumes?.length) {
          <p class="text-[--color-text-secondary]">巻情報がありません。</p>
        } @else {
          <ul class="divide-y divide-[--color-border]">
            @for (vol of series()!.volumes; track vol.volumeId) {
              <li class="py-3 flex items-center gap-3">
                @if (vol.thumbnailUrl) {
                  <img [src]="vol.thumbnailUrl" [alt]="series()!.title + ' 第' + (vol.volumeNumber ?? '?') + '巻'"
                       class="w-10 h-14 object-cover rounded shrink-0" loading="lazy" />
                }
                <div class="flex-1 min-w-0">
                  <p class="text-[--color-text-primary] font-medium">
                    @if (vol.volumeNumber) { 第{{ vol.volumeNumber }}巻 } @else { 単巻 }
                  </p>
                  <p class="text-sm text-[--color-text-secondary]">
                    {{ vol.releaseDate | releaseDate:vol.releaseDateIsMonthOnly }}
                  </p>
                </div>
                @if (vol.rakutenItemUrl) {
                  <a [href]="vol.rakutenItemUrl" target="_blank" rel="noopener noreferrer"
                     class="shrink-0 text-xs text-[--color-primary] underline">楽天で見る</a>
                }
              </li>
            }
          </ul>
        }
      } @else {
        <p class="text-[--color-text-secondary] py-16 text-center">シリーズが見つかりませんでした。</p>
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
    if (s.isSubscribed) {
      this.subscriptionsStore.unsubscribe(s.seriesId).subscribe({
        next: () => this.series.update(v => v ? { ...v, isSubscribed: false } : v),
      });
    } else {
      this.subscriptionsStore.subscribe(s.seriesId).subscribe({
        next: () => this.series.update(v => v ? { ...v, isSubscribed: true } : v),
      });
    }
  }
}
