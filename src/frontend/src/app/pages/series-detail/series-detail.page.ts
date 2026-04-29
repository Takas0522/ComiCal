import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { BadgeComponent } from '../../atoms/badge/badge.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { SeriesVolumeListComponent } from '../../organisms/series-volume-list/series-volume-list.component';
import { SeriesApi } from '../../core/api/series.api';
import { ToastService } from '../../core/services/toast.service';
import { addMonthsIso, todayIso } from '../../shared/format/jp-date';
import type { SeriesDetail } from '../../core/api/api-types';

@Component({
  selector: 'app-series-detail-page',
  standalone: true,
  imports: [
    PageLayoutComponent,
    BadgeComponent,
    SkeletonComponent,
    SeriesVolumeListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="シリーズ詳細" testid="series-detail">
      @if (loading()) {
        <div class="space-y-3" data-testid="series-detail-loading" aria-busy="true">
          <app-skeleton width="40%" height="2rem" />
          <app-skeleton width="100%" height="1rem" />
          <app-skeleton width="80%" height="1rem" />
        </div>
      } @else if (detail(); as d) {
        <header class="space-y-2" data-testid="series-detail-header">
          <h2 class="text-2xl font-bold" data-testid="series-detail-title">{{ d.series.title }}</h2>
          <div class="flex items-center gap-2">
            <app-badge
              [tone]="d.series.isCompleted ? 'success' : 'brand'"
              testid="series-detail-status"
            >{{ d.series.isCompleted ? '完結' : '連載中' }}</app-badge>
          </div>
        </header>
        <app-series-volume-list
          [volumes]="d.volumes"
          [seriesTitle]="d.series.title"
        />
      }
    </app-page-layout>
  `,
})
export class SeriesDetailPage {
  private readonly seriesApi = inject(SeriesApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  /** Bound from /series/:id via withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly detail = signal<SeriesDetail | null>(null);
  protected readonly loading = signal<boolean>(true);

  constructor() {
    effect(() => {
      const id = this.id();
      if (!id) return;
      const releaseFrom = addMonthsIso(todayIso(), -1);
      this.loading.set(true);
      this.seriesApi.getSeriesDetail(id, releaseFrom).subscribe({
        next: (d) => {
          this.detail.set(d);
          this.loading.set(false);
        },
        error: (err: { status?: number } | unknown) => {
          this.loading.set(false);
          const status = (err as { status?: number })?.status;
          if (status === 404) {
            this.toasts.show({
              title: 'シリーズが見つかりませんでした',
              severity: 'warning',
            });
            void this.router.navigate(['/search']);
          }
        },
      });
    });
  }
}
