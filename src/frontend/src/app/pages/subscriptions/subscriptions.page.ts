import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-subscriptions-page',
  standalone: true,
  imports: [SpinnerComponent, RouterLink],
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
          購読中のシリーズはありません。<br>
          <a routerLink="/search" class="text-[--color-primary] underline mt-2 inline-block">検索して追加する</a>
        </p>
      } @else {
        <ul class="divide-y divide-[--color-border]">
          @for (sub of store.items(); track sub.subscriptionId) {
            <li class="py-4 flex items-center justify-between gap-4">
              <a [routerLink]="['/series', sub.seriesId]"
                 class="flex-1 font-semibold text-[--color-text-primary] hover:text-[--color-primary] truncate">
                {{ sub.seriesTitle }}
              </a>
              <button
                type="button"
                class="shrink-0 px-3 py-1.5 text-sm rounded-lg border border-[--color-border] bg-[--color-surface-elevated] text-[--color-text-secondary] hover:bg-red-50 hover:text-red-600 transition-colors"
                (click)="unsubscribe(sub.seriesId)"
              >購読解除</button>
            </li>
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

  unsubscribe(seriesId: string) {
    this.store.unsubscribe(seriesId).subscribe({
      next: () => this.store.load(),
    });
  }
}
