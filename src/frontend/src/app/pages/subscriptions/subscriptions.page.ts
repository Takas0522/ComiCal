import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-subscriptions-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="購読" testid="subscriptions">
      <p class="text-sm text-[var(--color-muted)]" data-testid="subscriptions-placeholder">
        この画面は未実装のプレースホルダです。
      </p>
    </app-page-layout>
  `,
})
export class SubscriptionsPage {}
