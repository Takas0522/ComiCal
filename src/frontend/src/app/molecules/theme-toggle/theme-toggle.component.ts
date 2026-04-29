/// <reference types="@angular/localize" />
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

interface Option {
  readonly value: ThemeMode;
  readonly label: string;
  readonly testid: string;
}

/**
 * Segmented control for the light / dark / system theme.
 *
 * - Implemented as ARIA `radiogroup` with `aria-checked` per option.
 * - Supports keyboard navigation via Left/Right (and Up/Down) arrows.
 */
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      role="radiogroup"
      class="inline-flex rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] p-0.5"
      data-testid="theme-toggle"
      i18n-aria-label="@@settings.theme.aria"
      aria-label="表示テーマの切り替え"
      tabindex="-1"
      (keydown)="onKeydown($event)"
    >
      @for (opt of options; track opt.value) {
        <button
          type="button"
          role="radio"
          class="rounded-md px-3 py-1.5 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
          [class.bg-[var(--color-brand-500)]]="opt.value === value()"
          [class.text-white]="opt.value === value()"
          [class.text-[var(--color-fg)]]="opt.value !== value()"
          [attr.aria-checked]="opt.value === value()"
          [attr.tabindex]="opt.value === value() ? 0 : -1"
          [attr.data-testid]="opt.testid"
          (click)="select(opt.value)"
        >
          {{ opt.label }}
        </button>
      }
    </div>
  `,
})
export class ThemeToggleComponent {
  readonly value = input.required<ThemeMode>();
  readonly valueChange = output<ThemeMode>();

  protected readonly options: readonly Option[] = [
    { value: 'light', label: $localize`:@@settings.theme.light:ライト`, testid: 'theme-toggle-light' },
    { value: 'dark', label: $localize`:@@settings.theme.dark:ダーク`, testid: 'theme-toggle-dark' },
    { value: 'system', label: $localize`:@@settings.theme.system:システム`, testid: 'theme-toggle-system' },
  ];

  protected readonly currentIndex = computed(() =>
    this.options.findIndex((o) => o.value === this.value()),
  );

  protected select(v: ThemeMode): void {
    if (v !== this.value()) {
      this.valueChange.emit(v);
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target || target.getAttribute('role') !== 'radio') return;
    const idx = this.currentIndex();
    let next = idx;
    if (event.key === 'ArrowRight' || event.key === 'ArrowDown') {
      next = (idx + 1) % this.options.length;
    } else if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') {
      next = (idx - 1 + this.options.length) % this.options.length;
    } else {
      return;
    }
    event.preventDefault();
    const nextOpt = this.options[next];
    this.valueChange.emit(nextOpt.value);
    queueMicrotask(() => {
      const root = target.parentElement;
      const btn = root?.querySelector<HTMLButtonElement>(
        `[data-testid="${nextOpt.testid}"]`,
      );
      btn?.focus();
    });
  }
}
