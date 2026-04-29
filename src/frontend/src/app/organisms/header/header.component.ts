/// <reference types="@angular/localize" />
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import { AnonymousStoreService } from '../../core/anonymous-store';
import { AuthService } from '../../core/auth';

interface NavItem {
  readonly label: string;
  readonly path: string;
  readonly testid: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, SearchBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header
      class="sticky top-0 z-10 border-b border-[var(--color-border)] bg-[var(--color-surface)]"
      data-testid="app-header"
    >
      <div class="mx-auto max-w-6xl flex flex-wrap items-center justify-between gap-3 p-4">
        <a routerLink="/" class="text-lg font-bold" data-testid="header-logo">
          {{ title() }}
        </a>
        <nav i18n-aria-label="@@nav.primary.label" aria-label="メインナビゲーション">
          <ul class="flex items-center gap-4 text-sm">
            @for (item of navItems(); track item.path) {
              <li>
                <a
                  [routerLink]="item.path"
                  routerLinkActive="text-[var(--color-brand-500)]"
                  [routerLinkActiveOptions]="{ exact: item.path === '/' }"
                  [attr.data-testid]="item.testid"
                  class="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)] rounded"
                >{{ item.label }}</a>
              </li>
            }
            @if (localCount() > 0) {
              <li>
                <span
                  class="inline-flex items-center rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] px-2 py-0.5 text-xs font-medium text-[var(--color-brand-700)]"
                  data-testid="local-entries-badge"
                  [attr.aria-label]="'端末内の保存数: ' + localCount()"
                  title="端末内の保存数"
                >☆ {{ localCount() }}</span>
              </li>
            }
          </ul>
        </nav>
        <div class="w-full md:w-80">
          <app-search-bar (searchTerm)="onSearch($event)" />
        </div>
        <div class="flex items-center gap-2" data-testid="header-auth">
          @if (isAuthenticated()) {
            <span
              class="text-sm text-[var(--color-fg)]"
              data-testid="header-user-name"
              [attr.aria-label]="'サインイン中: ' + (displayName() ?? '')"
            >{{ displayName() }}</span>
            <a
              [href]="logoutHref()"
              class="rounded border border-[var(--color-border)] px-3 py-1 text-sm hover:bg-[var(--color-surface)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
              data-testid="header-logout"
              i18n="@@header.logout"
            >ログアウト</a>
          } @else {
            <a
              routerLink="/login"
              class="rounded border border-[var(--color-border)] px-3 py-1 text-sm hover:bg-[var(--color-surface)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
              data-testid="header-login"
              i18n="@@header.login"
            >ログイン</a>
          }
        </div>
      </div>
    </header>
  `,
})
export class HeaderComponent {
  readonly title = input.required<string>();

  private readonly router = inject(Router);
  private readonly store = inject(AnonymousStoreService);
  private readonly auth = inject(AuthService);

  protected readonly localCount = this.store.totalLocalEntries;
  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly displayName = this.auth.displayName;
  protected readonly logoutHref = computed(() => this.auth.logoutUrl('/'));

  protected readonly navItems = computed<readonly NavItem[]>(() => [
    { label: 'ホーム', path: '/', testid: 'nav-home' },
    { label: $localize`:@@nav.calendar:カレンダー`, path: '/calendar', testid: 'nav-calendar' },
    { label: '検索', path: '/search', testid: 'nav-search' },
    { label: '購読', path: '/subscriptions', testid: 'nav-subscriptions' },
    { label: '設定', path: '/settings', testid: 'nav-settings' },
  ]);

  protected onSearch(term: string): void {
    void this.router.navigate(['/search'], { queryParams: { q: term } });
  }
}
