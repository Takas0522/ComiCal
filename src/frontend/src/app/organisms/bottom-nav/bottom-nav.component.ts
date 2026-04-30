import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav
      data-testid="bottom-nav"
      aria-label="メインナビゲーション"
      class="fixed bottom-0 left-0 right-0 z-50 bg-[--color-surface] border-t border-[--color-border] safe-area-bottom"
    >
      <ul class="flex justify-around items-center h-16 container mx-auto px-2">
        @for (item of navItems; track item.path) {
          <li class="flex-1">
            <a
              [routerLink]="item.path"
              routerLinkActive="text-[--color-primary]"
              [routerLinkActiveOptions]="{ exact: item.path === '/' }"
              class="flex flex-col items-center justify-center gap-0.5 py-2 text-[--color-text-secondary] transition-colors hover:text-[--color-text-primary]"
              [attr.aria-label]="item.label"
            >
              <span class="text-xl" aria-hidden="true">{{ item.icon }}</span>
              <span class="text-xs">{{ item.label }}</span>
            </a>
          </li>
        }
      </ul>
    </nav>
  `,
})
export class BottomNavComponent {
  protected readonly navItems: NavItem[] = [
    { label: 'ホーム', path: '/', icon: '🏠' },
    { label: 'カレンダー', path: '/calendar', icon: '📅' },
    { label: '検索', path: '/search', icon: '🔍' },
    { label: '購読', path: '/subscriptions', icon: '⭐' },
  ];
}
