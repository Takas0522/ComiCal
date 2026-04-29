import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type BadgeTone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger';

@Component({
  selector: 'app-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="classes()" [attr.data-testid]="testid()">
      <ng-content />
    </span>
  `,
})
export class BadgeComponent {
  readonly tone = input<BadgeTone>('neutral');
  readonly testid = input<string>('badge');

  protected readonly classes = computed(() => {
    const base =
      'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium border';
    switch (this.tone()) {
      case 'brand':
        return `${base} bg-[var(--color-brand-500)]/10 text-[var(--color-brand-700)] border-[var(--color-brand-500)]/30`;
      case 'success':
        return `${base} bg-emerald-500/10 text-emerald-700 border-emerald-500/30`;
      case 'warning':
        return `${base} bg-amber-500/10 text-amber-700 border-amber-500/30`;
      case 'danger':
        return `${base} bg-rose-500/10 text-rose-700 border-rose-500/30`;
      case 'neutral':
      default:
        return `${base} bg-[var(--color-surface)] text-[var(--color-muted)] border-[var(--color-border)]`;
    }
  });
}
