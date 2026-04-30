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
      <div data-testid="page-settings" class="py-6 max-w-lg">
        <h1 class="text-2xl font-bold text-[--color-text-primary] mb-8">設定</h1>

        <section class="mb-8">
          <h2 class="text-lg font-semibold text-[--color-text-primary] mb-4">テーマ</h2>
          <fieldset class="flex gap-3">
            <legend class="sr-only">テーマを選択</legend>
            @for (option of themeOptions; track option.value) {
              <label class="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="theme"
                  [value]="option.value"
                  [checked]="store.theme() === option.value"
                  (change)="store.setTheme(option.value)"
                  class="accent-[--color-primary]"
                />
                <span class="text-sm text-[--color-text-primary]">{{ option.label }}</span>
              </label>
            }
          </fieldset>
        </section>

        <section>
          <h2 class="text-lg font-semibold text-[--color-text-primary] mb-4">その他</h2>
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
