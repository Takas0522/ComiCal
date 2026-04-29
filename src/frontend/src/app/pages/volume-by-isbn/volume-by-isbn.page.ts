import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { VolumeApi } from '../../core/api/volume.api';
import { ToastService } from '../../core/services/toast.service';
import { formatJpDate } from '../../shared/format/jp-date';
import type { Volume } from '../../core/api/api-types';

@Component({
  selector: 'app-volume-by-isbn-page',
  standalone: true,
  imports: [PageLayoutComponent, SkeletonComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="巻情報" testid="volume-by-isbn">
      @if (loading()) {
        <div class="space-y-3" aria-busy="true" data-testid="volume-by-isbn-loading">
          <app-skeleton width="40%" height="2rem" />
          <app-skeleton width="60%" height="1rem" />
        </div>
      } @else if (volume(); as v) {
        <article class="space-y-3" data-testid="volume-by-isbn-card">
          <h2 class="text-xl font-semibold" data-testid="volume-by-isbn-isbn">
            ISBN {{ v.isbn }}
          </h2>
          @if (v.volumeNumber !== null && v.volumeNumber !== undefined) {
            <p data-testid="volume-by-isbn-volume">
              <span i18n="@@volumeByIsbn.volume">巻数:</span>
              第{{ v.volumeNumber }}巻
            </p>
          }
          <p data-testid="volume-by-isbn-release">
            <span i18n="@@volumeByIsbn.release">発売日:</span>
            {{ release(v) }}
          </p>
          <p>
            <a
              [routerLink]="['/series', v.seriesId]"
              data-testid="volume-by-isbn-series-link"
              class="text-[var(--color-brand-700)] hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
              i18n="@@volumeByIsbn.gotoSeries"
            >シリーズの一覧を見る</a>
          </p>
        </article>
      }
    </app-page-layout>
  `,
})
export class VolumeByIsbnPage {
  private readonly volumeApi = inject(VolumeApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  readonly isbn = input.required<string>();

  protected readonly volume = signal<Volume | null>(null);
  protected readonly loading = signal<boolean>(true);

  constructor() {
    effect(() => {
      const isbn = this.isbn();
      if (!isbn) return;
      this.loading.set(true);
      this.volumeApi.getVolumeByIsbn(isbn).subscribe({
        next: (v) => {
          this.volume.set(v);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          const status = (err as { status?: number })?.status;
          if (status === 404) {
            this.toasts.show({
              title: '指定された ISBN の巻は見つかりませんでした',
              severity: 'warning',
            });
            void this.router.navigate(['/search']);
          }
        },
      });
    });
  }

  protected release(v: Volume): string {
    return formatJpDate(v.releaseDate, v.releaseDateIsMonthOnly);
  }
}
