import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';

import { VolumeCardComponent } from '../../molecules/volume-card/volume-card.component';
import { todayIso } from '../../shared/format/jp-date';
import type { CalendarDay, CalendarDto } from '../../core/api/api-types';

interface MonthGroup {
  readonly key: string;
  readonly label: string;
  readonly days: readonly CalendarDay[];
}

const WEEKDAY = ['日', '月', '火', '水', '木', '金', '土'] as const;

function monthKey(iso: string): string {
  return iso.slice(0, 7);
}

function monthLabel(yyyyMm: string): string {
  const m = /^(\d{4})-(\d{2})$/.exec(yyyyMm);
  return m ? `${m[1]}年${m[2]}月` : yyyyMm;
}

function dayLabel(iso: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!m) return iso;
  const [, y, mo, d] = m;
  const dt = new Date(Date.UTC(Number(y), Number(mo) - 1, Number(d)));
  const w = WEEKDAY[dt.getUTCDay()];
  return `${Number(d)}日 (${w})`;
}

function addMonth(yyyyMm: string, delta: number): string {
  const [y, m] = yyyyMm.split('-').map(Number);
  const dt = new Date(Date.UTC(y, m - 1 + delta, 1));
  return `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}`;
}

@Component({
  selector: 'app-calendar-grid',
  standalone: true,
  imports: [VolumeCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3"
      data-testid="calendar-grid"
    >
      @for (month of months(); track month.key) {
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden"
          data-testid="calendar-month"
          [attr.data-month]="month.key"
          [attr.aria-label]="month.label"
        >
          <header
            class="sticky top-0 z-[1] border-b border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm font-semibold"
            data-testid="calendar-month-heading"
          >
            {{ month.label }}
          </header>
          <div class="divide-y divide-[var(--color-border)]">
            @for (day of month.days; track day.date) {
              <div
                class="p-3"
                [class.ring-2]="day.date === today()"
                [class.ring-[var(--color-brand-500)]]="day.date === today()"
                [class.rounded-md]="day.date === today()"
                data-testid="calendar-day"
                [attr.data-date]="day.date"
                [attr.data-today]="day.date === today() ? 'true' : null"
              >
                <h3
                  class="sticky top-9 z-[1] mb-2 bg-[var(--color-surface)] py-1 text-xs font-semibold text-[var(--color-muted)]"
                  data-testid="calendar-day-date"
                >
                  {{ dayLabel(day.date) }}
                </h3>
                <ul class="grid grid-cols-2 gap-2 sm:grid-cols-3">
                  @for (volume of day.volumes; track volume.id) {
                    <li data-testid="calendar-volume">
                      <app-volume-card [volume]="volume" compact="true" />
                    </li>
                  }
                </ul>
              </div>
            }
          </div>
        </section>
      }
    </div>
  `,
})
export class CalendarGridComponent {
  readonly calendar = input.required<CalendarDto>();
  /** Override "today" for deterministic tests/SSR. ISO `yyyy-MM-dd`. */
  readonly today = input<string>(todayIso());

  protected readonly months = computed<readonly MonthGroup[]>(() => {
    const c = this.calendar();
    const startMonth = monthKey(c.monthFrom);
    // Bucket days by month; only emit non-empty months.
    const buckets = new Map<string, CalendarDay[]>();
    for (const day of c.days) {
      if (day.volumes.length === 0) continue;
      const key = monthKey(day.date);
      const arr = buckets.get(key) ?? [];
      arr.push(day);
      buckets.set(key, arr);
    }
    const groups: MonthGroup[] = [];
    for (let i = 0; i < c.monthCount; i++) {
      const key = addMonth(startMonth, i);
      const days = buckets.get(key);
      if (!days || days.length === 0) continue;
      const sorted = [...days].sort((a, b) => a.date.localeCompare(b.date));
      groups.push({ key, label: monthLabel(key), days: sorted });
    }
    return groups;
  });

  protected dayLabel(iso: string): string {
    return dayLabel(iso);
  }
}
