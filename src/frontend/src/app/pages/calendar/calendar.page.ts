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
    <div data-testid="page-calendar" class="py-6">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-[--color-text-primary]">カレンダー</h1>
        <div class="flex rounded-lg border border-[--color-border] overflow-hidden" role="group" aria-label="表示切替">
          <button type="button" (click)="view.set('week')"
            [class.bg-\[--color-primary\]]="view() === 'week'"
            [class.text-white]="view() === 'week'"
            class="px-4 py-2 text-sm transition-colors"
            [attr.aria-pressed]="view() === 'week'">週</button>
          <button type="button" (click)="view.set('month')"
            [class.bg-\[--color-primary\]]="view() === 'month'"
            [class.text-white]="view() === 'month'"
            class="px-4 py-2 text-sm transition-colors border-l border-[--color-border]"
            [attr.aria-pressed]="view() === 'month'">月</button>
        </div>
      </div>

      @if (isLoading()) {
        <div class="flex justify-center py-16"><app-spinner /></div>
      } @else if (calendarDays().length === 0 && undatedVolumes().length === 0) {
        <p class="text-[--color-text-secondary] text-center py-16">発売予定がありません。</p>
      } @else {
        @for (day of calendarDays(); track day.date) {
          <section class="mb-6">
            <h2 class="text-sm font-semibold text-[--color-text-secondary] mb-2 sticky top-0 bg-[--color-bg] py-1">
              {{ day.date | releaseDate:false }}
            </h2>
            <ul class="divide-y divide-[--color-border] rounded-lg border border-[--color-border] overflow-hidden">
              @for (vol of day.volumes; track vol.volumeId) {
                <li class="px-4 py-3 flex items-center gap-3 bg-[--color-surface]">
                  @if (vol.thumbnailUrl) {
                    <img [src]="vol.thumbnailUrl" class="w-8 h-11 object-cover rounded shrink-0" loading="lazy" alt="" />
                  }
                  <a [routerLink]="['/series', vol.series?.seriesId]"
                     class="flex-1 text-[--color-text-primary] hover:text-[--color-primary]">
                    {{ vol.series?.title ?? '不明' }}
                    @if (vol.volumeNumber) { <span class="text-sm text-[--color-text-secondary]"> 第{{ vol.volumeNumber }}巻</span> }
                  </a>
                </li>
              }
            </ul>
          </section>
        }
        @if (undatedVolumes().length > 0) {
          <section class="mb-6">
            <h2 class="text-sm font-semibold text-[--color-text-secondary] mb-2">発売日未定</h2>
            <ul class="divide-y divide-[--color-border] rounded-lg border border-[--color-border] overflow-hidden">
              @for (vol of undatedVolumes(); track vol.volumeId) {
                <li class="px-4 py-3 flex items-center gap-3 bg-[--color-surface]">
                  <a [routerLink]="['/series', vol.series?.seriesId]"
                     class="flex-1 text-[--color-text-primary] hover:text-[--color-primary]">
                    {{ vol.series?.title ?? '不明' }}
                    @if (vol.volumeNumber) { <span class="text-sm text-[--color-text-secondary]"> 第{{ vol.volumeNumber }}巻</span> }
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
