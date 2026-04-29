import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-center justify-center gap-2 p-8 text-center text-[var(--color-muted)]"
      role="status"
      [attr.data-testid]="testid()"
    >
      <span aria-hidden="true" class="text-3xl">{{ icon() }}</span>
      <p class="text-sm">{{ message() }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  readonly message = input.required<string>();
  readonly icon = input<string>('📭');
  readonly testid = input<string>('empty-state');
}
