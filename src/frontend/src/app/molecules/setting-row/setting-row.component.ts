import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Labelled row for the settings page. Renders a label (and optional
 * description) on the left and projects an arbitrary control on the right.
 *
 * Stacks vertically on narrow viewports.
 */
@Component({
  selector: 'app-setting-row',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col gap-3 border-b border-[var(--color-border)] py-4 last:border-b-0 sm:flex-row sm:items-center sm:justify-between sm:gap-6"
      [attr.data-testid]="'setting-row'"
      [attr.data-testid-key]="testidKey()"
    >
      <div class="min-w-0 flex-1">
        <p class="text-sm font-medium text-[var(--color-fg)]" [attr.data-testid]="'setting-row-label'">
          {{ label() }}
        </p>
        @if (description(); as d) {
          <p
            class="mt-1 text-xs text-[var(--color-muted)]"
            [attr.data-testid]="'setting-row-description'"
          >
            {{ d }}
          </p>
        }
      </div>
      <div class="shrink-0 sm:text-right" [attr.data-testid]="'setting-row-control'">
        <ng-content />
      </div>
    </div>
  `,
})
export class SettingRowComponent {
  readonly label = input.required<string>();
  readonly description = input<string | undefined>(undefined);
  readonly testidKey = input<string | undefined>(undefined);
}
