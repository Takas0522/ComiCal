import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [attr.data-testid]="'btn-' + intent()"
      [disabled]="disabled() || loading()"
      [class]="buttonClasses()"
      type="button"
    >
      @if (loading()) {
        <span
          class="inline-block w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin mr-2"
          aria-hidden="true"
        ></span>
      }
      <ng-content />
    </button>
  `,
})
export class ButtonComponent {
  readonly intent = input('button');
  readonly variant = input<'primary' | 'secondary' | 'ghost'>('primary');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly disabled = input(false);
  readonly loading = input(false);

  buttonClasses() {
    const base =
      'inline-flex items-center justify-center font-medium rounded transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';
    const sizes: Record<string, string> = {
      sm: 'px-3 py-1.5 text-sm',
      md: 'px-4 py-2 text-base',
      lg: 'px-6 py-3 text-lg',
    };
    const variants: Record<string, string> = {
      primary:
        'bg-[--color-primary] text-white hover:bg-[--color-primary-hover] focus-visible:outline-[--color-primary]',
      secondary:
        'bg-[--color-surface-elevated] text-[--color-text-primary] border border-[--color-border] hover:bg-[--color-border]',
      ghost: 'text-[--color-text-primary] hover:bg-[--color-surface-elevated]',
    };
    return `${base} ${sizes[this.size()]} ${variants[this.variant()]}`;
  }
}
