import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <article
      data-testid="card-root"
      class="rounded-lg border border-slate-200 bg-white p-4 shadow-sm"
    >
      <h3 data-testid="card-title" class="text-lg font-semibold">{{ title() }}</h3>
      @if (description(); as desc) {
        <p data-testid="card-description" class="mt-2 text-sm text-slate-600">{{ desc }}</p>
      }
      <button
        type="button"
        data-testid="card-action"
        class="mt-3 rounded bg-indigo-600 px-3 py-1 text-white"
        (click)="actionClicked.emit()"
      >
        {{ actionLabel() }}
      </button>
    </article>
  `,
})
export class CardComponent {
  readonly title = input.required<string>();
  readonly description = input<string | undefined>(undefined);
  readonly actionLabel = input<string>('OK');
  readonly actionClicked = output<void>();
}
