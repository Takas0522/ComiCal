import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-skip-link',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      [href]="'#' + targetId()"
      class="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-[var(--color-brand-500)] focus:px-3 focus:py-1 focus:text-white focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[var(--color-brand-500)]"
      data-testid="skip-link"
      i18n="@@a11y.skipLink"
    >メインコンテンツへスキップ</a>
  `,
})
export class SkipLinkComponent {
  readonly targetId = input<string>('main-content');
}
