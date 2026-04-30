import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
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
  styles: [`
    .nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 3px;
      padding: 6px 0;
      color: var(--color-text-tertiary);
      transition: color 0.15s;
      position: relative;
      flex: 1;
    }
    .nav-item:hover { color: var(--color-text-secondary); }
    .nav-item.active {
      color: var(--color-primary);
    }
    .nav-icon {
      width: 26px;
      height: 26px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
      transition: background 0.15s;
      font-size: 1.125rem;
    }
    .nav-item.active .nav-icon {
      background: var(--color-primary-light);
    }
    .nav-label { font-size: 0.625rem; font-weight: 500; letter-spacing: 0.01em; }
  `],
  template: `
    <nav
      data-testid="bottom-nav"
      aria-label="メインナビゲーション"
      class="fixed bottom-0 left-0 right-0 z-50 safe-area-bottom"
      style="
        background: var(--color-surface-glass);
        backdrop-filter: saturate(180%) blur(20px);
        -webkit-backdrop-filter: saturate(180%) blur(20px);
        box-shadow: var(--shadow-nav);
      "
    >
      <ul class="flex justify-around items-center h-16 container mx-auto px-2">
        @for (item of navItems; track item.path) {
          <li class="flex-1 flex justify-center">
            <a
              [routerLink]="item.path"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: item.path === '/' }"
              class="nav-item"
              [attr.aria-label]="item.label"
            >
              <span class="nav-icon" aria-hidden="true">{{ item.icon }}</span>
              <span class="nav-label">{{ item.label }}</span>
            </a>
          </li>
        }
      </ul>
    </nav>
  `,
})
export class BottomNavComponent {
  protected readonly navItems: NavItem[] = [
    { label: 'ホーム',   path: '/',              icon: '🏠' },
    { label: 'カレンダー', path: '/calendar',    icon: '📅' },
    { label: '検索',     path: '/search',        icon: '🔍' },
    { label: '購読',     path: '/subscriptions', icon: '⭐' },
  ];
}
