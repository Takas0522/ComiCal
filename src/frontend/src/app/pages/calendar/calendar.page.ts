import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReleaseDatePipe } from '../../shared/pipes/release-date.pipe';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { RouterLink } from '@angular/router';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

type CalendarView = 'week' | 'month';

interface CalendarVolume {
  volumeId: string;
  volumeNumber?: number | null;
  releaseDate: string | null;
  releaseDateIsMonthOnly: boolean;
  thumbnailUrl?: string | null;
  series?: { seriesId: string; title: string };
}

interface CalendarDay {
  date: string;
  volumes: CalendarVolume[];
}

interface CalendarData {
  days: CalendarDay[];
  undatedVolumes: CalendarVolume[];
}

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [ReleaseDatePipe, SpinnerComponent, RouterLink, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
    <div data-testid="page-calendar" class="py-5">
      <div class="flex items-center justify-between mb-5">
        <h1 class="text-xl font-bold" style="color: var(--color-text-primary)">カレンダー</h1>
        <div class="flex p-1 rounded-xl gap-1" style="background: var(--color-surface-elevated)" role="group" aria-label="表示切替">
          <button type="button" (click)="view.set('week')"
            class="px-4 py-1.5 text-sm font-medium rounded-lg transition-all"
            [style]="view() === 'week'
              ? 'background: white; color: var(--color-primary); box-shadow: 0 1px 3px rgba(0,0,0,0.12)'
              : 'background: transparent; color: var(--color-text-secondary)'"
            [attr.aria-pressed]="view() === 'week'">週</button>
          <button type="button" (click)="view.set('month')"
            class="px-4 py-1.5 text-sm font-medium rounded-lg transition-all"
            [style]="view() === 'month'
              ? 'background: white; color: var(--color-primary); box-shadow: 0 1px 3px rgba(0,0,0,0.12)'
              : 'background: transparent; color: var(--color-text-secondary)'"
            [attr.aria-pressed]="view() === 'month'">月</button>
        </div>
      </div>

      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (calendarDays().length === 0 && undatedVolumes().length === 0) {
        <p class="text-center py-16" style="color: var(--color-text-secondary)">発売予定がありません。</p>
      } @else {
        @for (day of calendarDays(); track day.date) {
          <section class="mb-5">
            <h2 class="text-xs font-semibold mb-2 sticky top-14 py-1 px-2 rounded-md inline-block"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)">
              {{ day.date | releaseDate:false }}
            </h2>
            <ul class="flex flex-col gap-1.5">
              @for (vol of day.volumes; track vol.volumeId) {
                <li class="px-4 py-3 flex items-center gap-3 rounded-xl" style="background: var(--color-surface); box-shadow: var(--shadow-card)">
                  @if (vol.thumbnailUrl) {
                    <img [src]="vol.thumbnailUrl" class="w-8 h-11 object-cover rounded-lg shrink-0" loading="lazy" alt="" />
                  }
                  <a [routerLink]="['/series', vol.series?.seriesId]"
                     style="color: var(--color-text-primary)" class="flex-1">
                    {{ vol.series?.title ?? '不明' }}
                    @if (vol.volumeNumber) { <span class="text-sm" style="color: var(--color-text-secondary)"> 第{{ vol.volumeNumber }}巻</span> }
                  </a>
                </li>
              }
            </ul>
          </section>
        }
        @if (undatedVolumes().length > 0) {
          <section class="mb-5">
            <h2 class="text-xs font-semibold mb-2 px-2 py-1 rounded-md inline-block"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)">発売日未定</h2>
            <ul class="flex flex-col gap-1.5">
              @for (vol of undatedVolumes(); track vol.volumeId) {
                <li class="px-4 py-3 flex items-center gap-3 rounded-xl" style="background: var(--color-surface); box-shadow: var(--shadow-card)">
                  <a [routerLink]="['/series', vol.series?.seriesId]"
                     style="color: var(--color-text-primary)" class="flex-1">
                    {{ vol.series?.title ?? '不明' }}
                    @if (vol.volumeNumber) { <span class="text-sm" style="color: var(--color-text-secondary)"> 第{{ vol.volumeNumber }}巻</span> }
                  </a>
                </li>
              }
            </ul>
          </section>
        }
      }
    </div>
    </app-page-layout>
  `,
})
export class CalendarPage implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly view = signal<CalendarView>('week');
  protected readonly calendarDays = signal<CalendarDay[]>([]);
  protected readonly undatedVolumes = signal<CalendarVolume[]>([]);
  protected readonly isLoading = signal(false);

  ngOnInit() {
    this.isLoading.set(true);
    this.http.get<CalendarData>('/api/v1/volumes/calendar').subscribe({
      next: d => {
        this.calendarDays.set(d.days ?? []);
        this.undatedVolumes.set(d.undatedVolumes ?? []);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
