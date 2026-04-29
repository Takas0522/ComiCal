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
import { MonthNavigatorComponent } from '../../molecules/month-navigator/month-navigator.component';
import { CalendarGridComponent } from '../../organisms/calendar-grid/calendar-grid.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { EmptyStateComponent } from '../../atoms/empty-state/empty-state.component';
import { CalendarApi } from '../../core/api/calendar.api';
import { todayIso } from '../../shared/format/jp-date';
import type { CalendarDto } from '../../core/api/api-types';

const DEFAULT_MONTH_COUNT = 3;

function currentMonth(): string {
  return todayIso().slice(0, 7);
}

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [
    PageLayoutComponent,
    MonthNavigatorComponent,
    CalendarGridComponent,
    SkeletonComponent,
    EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="発売カレンダー" i18n-heading="@@calendar.heading" testid="calendar">
      <app-month-navigator
        [value]="month()"
        (valueChange)="onMonthChange($event)"
      />

      @if (loading()) {
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3" data-testid="calendar-loading">
          <app-skeleton height="12rem" />
          <app-skeleton height="12rem" />
          <app-skeleton height="12rem" />
        </div>
      } @else if (calendar(); as cal) {
        @if (hasVolumes()) {
          <app-calendar-grid [calendar]="cal" />
        } @else {
          <app-empty-state
            message="この期間に発売予定はありません。"
            i18n-message="@@calendar.empty"
            testid="calendar-empty"
          />
        }
      }
    </app-page-layout>
  `,
})
export class CalendarPage {
  private readonly api = inject(CalendarApi);
  private readonly router = inject(Router);

  /** Bound from query param `monthFrom`. */
  readonly monthFrom = input<string | undefined>(undefined);

  protected readonly month = computed(() => this.monthFrom() ?? currentMonth());
  protected readonly calendar = signal<CalendarDto | null>(null);
  protected readonly loading = signal<boolean>(false);

  protected readonly hasVolumes = computed(() => {
    const c = this.calendar();
    if (!c) return false;
    return c.days.some((d) => d.volumes.length > 0);
  });

  constructor() {
    effect(() => {
      const m = this.month();
      this.loading.set(true);
      this.calendar.set(null);
      this.api.getCalendar({ monthFrom: m, monthCount: DEFAULT_MONTH_COUNT }).subscribe({
        next: (c) => {
          this.calendar.set(c);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });
  }

  protected onMonthChange(value: string): void {
    void this.router.navigate(['/calendar'], {
      queryParams: { monthFrom: value },
      queryParamsHandling: 'merge',
    });
  }
}
