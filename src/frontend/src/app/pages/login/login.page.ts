import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

interface LoginProvider {
  name: string;
  path: string;
  label: string;
}

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-login" class="py-12 flex flex-col items-center">
        <!-- Logo mark -->
        <span
          class="inline-flex items-center justify-center w-16 h-16 rounded-2xl text-3xl font-bold text-white mb-5"
          style="background: linear-gradient(135deg, #e8002d 0%, #ff3b5c 100%); box-shadow: 0 4px 20px rgba(232,0,45,0.4)"
          aria-hidden="true"
        >漫</span>

        <h1 class="text-xl font-bold mb-1" style="color: var(--color-text-primary)">まんがリマインダー</h1>
        <p class="text-sm mb-8" style="color: var(--color-text-secondary)">アカウントでログインしてください</p>

        <div class="flex flex-col gap-3 w-full max-w-xs">
          @for (provider of providers; track provider.path) {
            <a
              [href]="provider.path"
              class="flex items-center justify-center gap-3 px-4 py-3 rounded-xl font-medium text-sm transition-all"
              style="background: var(--color-surface); border: 1px solid var(--color-border); color: var(--color-text-primary); box-shadow: var(--shadow-card)"
              [attr.data-testid]="'btn-login-' + provider.name"
            >
              {{ provider.label }}
            </a>
          }
        </div>
      </div>
    </app-page-layout>
  `,
})
export class LoginPage {
  protected readonly providers: LoginProvider[] = [
    { name: 'aad', path: '/.auth/login/aad', label: 'Microsoft でログイン' },
    { name: 'google', path: '/.auth/login/google', label: 'Google でログイン' },
    { name: 'twitter', path: '/.auth/login/twitter', label: 'X (Twitter) でログイン' },
  ];
}
