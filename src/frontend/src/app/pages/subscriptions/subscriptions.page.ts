import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';

@Component({
  selector: 'app-subscriptions-page',
  standalone: true,
  imports: [SpinnerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-subscriptions" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">購読一覧</h1>
      @if (store.isLoading()) {
        <div class="flex justify-center py-16">
          <app-spinner />
        </div>
      } @else if (store.items().length === 0) {
        <p class="text-[--color-text-secondary] text-center py-16">
          購読中のシリーズはありません。
        </p>
      } @else {
        <ul class="divide-y divide-[--color-border]">
          @for (sub of store.items(); track sub.subscriptionId) {
            <li class="py-3 text-[--color-text-primary]">{{ sub.seriesTitle }}</li>
          }
        </ul>
      }
    </div>
  `,
})
export class SubscriptionsPage implements OnInit {
  protected readonly store = inject(SubscriptionsStore);

  ngOnInit() {
    this.store.load();
  }
}
