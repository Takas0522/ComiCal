import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';

import { addMonthsIso, todayIso } from '../../shared/format/jp-date';

/** Returns `yyyy-MM` for today. */
function currentMonth(now: Date = new Date()): string {
  return todayIso(now).slice(0, 7);
}

/** Adds `delta` months to a `yyyy-MM` value and returns `yyyy-MM`. */
function shiftMonth(value: string, delta: number): string {
  const iso = `${value}-01`;
  return addMonthsIso(iso, delta).slice(0, 7);
}

/** Formats `yyyy-MM` as `yyyy年MM月`. */
function formatMonth(value: string): string {
  const m = /^(\d{4})-(\d{2})$/.exec(value);
  return m ? `${m[1]}年${m[2]}月` : value;
}

@Component({
  selector: 'app-month-navigator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex items-center justify-between gap-2"
      role="group"
      i18n-aria-label="@@monthNavigator.label"
      aria-label="月切替"
      data-testid="month-navigator"
    >
      <button
        type="button"
        class="rounded border border-[var(--color-border)] px-3 py-1 text-sm hover:bg-[var(--color-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
        i18n-aria-label="@@monthNavigator.prev"
        aria-label="前月へ移動"
        data-testid="month-navigator-prev"
        (click)="onPrev()"
      >
        <span i18n="@@monthNavigator.prev.label">← 前月</span>
      </button>
      <button
        type="button"
        class="rounded px-3 py-1 text-sm font-semibold hover:bg-[var(--color-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
        i18n-aria-label="@@monthNavigator.current"
        aria-label="今月に戻す"
        data-testid="month-navigator-current"
        (click)="onCurrent()"
      >
        {{ label() }}
      </button>
      <button
        type="button"
        class="rounded border border-[var(--color-border)] px-3 py-1 text-sm hover:bg-[var(--color-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
        i18n-aria-label="@@monthNavigator.next"
        aria-label="来月へ移動"
        data-testid="month-navigator-next"
        (click)="onNext()"
      >
        <span i18n="@@monthNavigator.next.label">来月 →</span>
      </button>
    </div>
  `,
})
export class MonthNavigatorComponent {
  /** Currently selected month, `yyyy-MM`. */
  readonly value = input.required<string>();
  readonly valueChange = output<string>();

  protected readonly label = computed(() => formatMonth(this.value()));

  protected onPrev(): void {
    this.valueChange.emit(shiftMonth(this.value(), -1));
  }

  protected onNext(): void {
    this.valueChange.emit(shiftMonth(this.value(), 1));
  }

  protected onCurrent(): void {
    this.valueChange.emit(currentMonth());
  }
}
