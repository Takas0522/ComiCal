import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { SettingsStore, Theme } from '../../features/settings.store';
import { ToggleComponent } from '../../molecules/toggle/toggle.component';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [ToggleComponent, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-settings" class="py-5 max-w-lg">
        <h1 class="text-xl font-bold mb-6" style="color: var(--color-text-primary)">設定</h1>

        <section
          class="mb-4 p-4 rounded-xl"
          style="background: var(--color-surface); box-shadow: var(--shadow-card)"
        >
          <h2 class="text-sm font-semibold mb-3" style="color: var(--color-text-secondary)">
            テーマ
          </h2>
          <fieldset class="flex gap-2">
            <legend class="sr-only">テーマを選択</legend>
            @for (option of themeOptions; track option.value) {
              <label
                class="flex-1 flex items-center justify-center gap-1.5 py-2 px-3 rounded-lg cursor-pointer text-sm font-medium transition-all"
                [style]="
                  store.theme() === option.value
                    ? 'background: var(--color-primary-light); color: var(--color-primary); border: 1.5px solid var(--color-primary)'
                    : 'background: var(--color-surface-elevated); color: var(--color-text-secondary); border: 1.5px solid transparent'
                "
              >
                <input
                  type="radio"
                  name="theme"
                  [value]="option.value"
                  [checked]="store.theme() === option.value"
                  (change)="store.setTheme(option.value)"
                  class="sr-only"
                />
                {{ option.label }}
              </label>
            }
          </fieldset>
        </section>

        <section
          class="p-4 rounded-xl"
          style="background: var(--color-surface); box-shadow: var(--shadow-card)"
        >
          <h2 class="text-sm font-semibold mb-3" style="color: var(--color-text-secondary)">
            その他
          </h2>
          <app-toggle
            [checked]="store.affiliateLinkEnabled()"
            label="楽天アフィリエイトリンクを表示"
            (toggled)="store.toggleAffiliateLink()"
          />
        </section>
      </div>
    </app-page-layout>
  `,
})
export class SettingsPage {
  protected readonly store = inject(SettingsStore);
  protected readonly themeOptions: { value: Theme; label: string }[] = [
    { value: 'light', label: 'ライト' },
    { value: 'dark', label: 'ダーク' },
    { value: 'system', label: 'システム' },
  ];
}
