import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthStore } from '../../features/auth.store';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { ToggleComponent } from '../../molecules/toggle/toggle.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, ToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header
      data-testid="header"
      class="sticky top-0 z-50"
      style="
        background: var(--color-surface-glass);
        backdrop-filter: saturate(180%) blur(20px);
        -webkit-backdrop-filter: saturate(180%) blur(20px);
        border-bottom: 1px solid var(--color-border);
      "
    >
      <div class="container mx-auto px-4 h-14 flex items-center justify-between gap-4">
        <!-- Logo -->
        <a
          routerLink="/"
          class="flex items-center gap-2 shrink-0 group"
          aria-label="まんがリマインダー ホームへ"
        >
          <span
            class="inline-flex items-center justify-center w-7 h-7 rounded-lg text-white text-sm font-bold leading-none"
            style="background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); box-shadow: 0 2px 6px rgba(232,0,45,0.35)"
            aria-hidden="true"
            >漫</span
          >
          <span class="text-base font-bold" style="color: var(--color-text-primary)">
            まんがリマインダー
          </span>
        </a>

        <!-- Right controls -->
        <div class="flex items-center gap-3">
          @if (auth.isLoggedIn()) {
            <app-toggle
              [checked]="subscriptions.showSubscribedOnly()"
              label="購読中のみ"
              (toggled)="subscriptions.toggleSubscribedOnly()"
            />
            <span
              class="text-sm hidden sm:inline truncate max-w-28"
              style="color: var(--color-text-secondary)"
            >
              {{ auth.displayName() }}
            </span>
            <a
              href="/.auth/logout"
              class="text-sm px-3 py-1.5 rounded-full border transition-colors"
              style="color: var(--color-text-secondary); border-color: var(--color-border)"
              data-testid="btn-logout"
              >ログアウト</a
            >
          } @else {
            <a
              routerLink="/login"
              class="text-sm font-semibold px-4 py-1.5 rounded-full text-white btn-primary"
              data-testid="btn-login"
              >ログイン</a
            >
          }
        </div>
      </div>
    </header>
  `,
})
export class HeaderComponent {
  protected readonly auth = inject(AuthStore);
  protected readonly subscriptions = inject(SubscriptionsStore);
}
