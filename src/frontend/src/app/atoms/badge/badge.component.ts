import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      [attr.data-testid]="'badge-' + variant()"
      [class]="badgeClasses()"
    >
      <ng-content />
    </span>
  `,
})
export class BadgeComponent {
  readonly variant = input<'default' | 'success' | 'warning' | 'error'>('default');

  badgeClasses() {
    const base = 'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium';
    const variants: Record<string, string> = {
      default: 'bg-[--color-surface-elevated] text-[--color-text-secondary]',
      success: 'bg-green-100 text-green-800',
      warning: 'bg-amber-100 text-amber-800',
      error: 'bg-red-100 text-red-800',
    };
    return `${base} ${variants[this.variant()]}`;
  }
}
