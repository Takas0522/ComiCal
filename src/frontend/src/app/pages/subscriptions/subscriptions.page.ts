import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-subscriptions-page',
  standalone: true,
  imports: [SpinnerComponent, RouterLink, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-subscriptions" class="py-5">
        <h1 class="text-xl font-bold mb-4" style="color: var(--color-text-primary)">購読一覧</h1>
        @if (store.isLoading()) {
          <div class="flex justify-center py-16">
            <app-spinner />
          </div>
        } @else if (store.items().length === 0) {
          <div class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">⭐</p>
            <p style="color: var(--color-text-secondary)">購読中のシリーズはありません。</p>
            <a routerLink="/search"
               class="inline-block mt-4 px-5 py-2 rounded-full text-sm font-semibold text-white btn-primary"
            >検索して追加する</a>
          </div>
        } @else {
          <ul class="flex flex-col gap-2">
            @for (sub of store.items(); track sub.subscriptionId) {
              <li
                class="flex items-center justify-between gap-4 p-4 rounded-xl"
                style="background: var(--color-surface); box-shadow: var(--shadow-card)"
              >
                <a [routerLink]="['/series', sub.seriesId]"
                   class="flex-1 font-semibold truncate transition-colors"
                   style="color: var(--color-text-primary)"
                >{{ sub.seriesTitle }}</a>
                <button
                  type="button"
                  class="shrink-0 px-3 py-1.5 text-sm rounded-full transition-all"
                  style="background: var(--color-surface-elevated); color: var(--color-text-secondary); border: 1px solid var(--color-border)"
                  (click)="unsubscribe(sub.seriesId)"
                >購読解除</button>
              </li>
            }
          </ul>
        }
      </div>
    </app-page-layout>
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
