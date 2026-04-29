import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface TabItem<T extends string = string> {
  readonly id: T;
  readonly label: string;
  readonly testid?: string;
}

@Component({
  selector: 'app-tab-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex gap-2 border-b border-[var(--color-border)]"
      role="tablist"
      data-testid="tab-list"
    >
      @for (item of items(); track item.id) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="item.id === active()"
          [attr.data-testid]="item.testid ?? 'tab-' + item.id"
          (click)="tabChange.emit(item.id)"
          [class]="
            item.id === active()
              ? 'px-4 py-2 text-sm font-medium border-b-2 border-[var(--color-brand-500)] text-[var(--color-brand-700)]'
              : 'px-4 py-2 text-sm font-medium border-b-2 border-transparent text-[var(--color-muted)] hover:text-[var(--color-fg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]'
          "
        >{{ item.label }}</button>
      }
    </div>
  `,
})
export class TabListComponent {
  readonly items = input.required<readonly TabItem[]>();
  readonly active = input.required<string>();
  readonly tabChange = output<string>();
}
