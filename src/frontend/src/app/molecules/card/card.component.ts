import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
      [attr.data-testid]="testid()"
    >
      <h3 class="text-base font-semibold mb-2">{{ title() }}</h3>
      <ng-content />
    </article>
  `,
})
export class CardComponent {
  readonly title = input.required<string>();
  readonly testid = input<string>('card');
}
