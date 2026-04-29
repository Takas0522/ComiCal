import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost';

@Component({
  selector: 'app-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      [class]="classes()"
      [attr.aria-label]="label()"
      [attr.data-testid]="testid()"
      [disabled]="disabled()"
      (click)="clicked.emit()"
    >
      <ng-content />
      @if (loading()) {
        <span class="sr-only" i18n="@@common.button.loading">読み込み中</span>
      }
    </button>
  `,
})
export class ButtonComponent {
  readonly label = input.required<string>();
  readonly testid = input.required<string>();
  readonly variant = input<ButtonVariant>('primary');
  readonly disabled = input<boolean>(false);
  readonly loading = input<boolean>(false);
  readonly clicked = output<void>();

  protected readonly classes = computed(() => {
    const base =
      'inline-flex items-center justify-center rounded-lg px-4 py-2 text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-offset-2';
    switch (this.variant()) {
      case 'secondary':
        return `${base} bg-[var(--color-surface)] text-[var(--color-fg)] border border-[var(--color-border)] hover:bg-[var(--color-border)]`;
      case 'ghost':
        return `${base} bg-transparent text-[var(--color-fg)] hover:bg-[var(--color-surface)]`;
      case 'primary':
      default:
        return `${base} bg-[var(--color-brand-500)] text-white hover:bg-[var(--color-brand-700)]`;
    }
  });
}
