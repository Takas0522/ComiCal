import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-series-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-series-detail" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-2">シリーズ詳細</h1>
      <p class="text-sm text-[--color-text-secondary] mb-6">ID: {{ id() }}</p>
      <p class="text-[--color-text-secondary]">全巻リストと購読・購入操作ができます。</p>
    </div>
  `,
})
export class SeriesDetailPage {
  readonly id = input('');
}
