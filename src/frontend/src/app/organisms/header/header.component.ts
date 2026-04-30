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
      class="sticky top-0 z-50 bg-[--color-surface] border-b border-[--color-border] shadow-sm"
    >
      <div class="container mx-auto px-4 h-14 flex items-center justify-between gap-4">
        <a routerLink="/" class="text-lg font-bold text-[--color-primary] shrink-0">
          まんがリマインダー
        </a>

        <div class="flex items-center gap-3">
          @if (auth.isLoggedIn()) {
            <app-toggle
              [checked]="subscriptions.showSubscribedOnly()"
              label="購読中のみ"
              (toggled)="subscriptions.toggleSubscribedOnly()"
            />
            <span class="text-sm text-[--color-text-secondary] hidden sm:inline truncate max-w-32">
              {{ auth.displayName() }}
            </span>
            <a
              href="/.auth/logout"
              class="text-sm text-[--color-text-secondary] hover:text-[--color-text-primary] whitespace-nowrap"
              data-testid="btn-logout"
            >ログアウト</a>
          } @else {
            <a
              routerLink="/login"
              class="text-sm bg-[--color-primary] text-white px-3 py-1.5 rounded hover:bg-[--color-primary-hover] transition-colors"
              data-testid="btn-login"
            >ログイン</a>
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
