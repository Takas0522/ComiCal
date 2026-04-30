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
        <h1 class="text-2xl font-bold text-[--color-text-primary] mb-2">ログイン</h1>
        <p class="text-[--color-text-secondary] mb-10">アカウントでログインしてください</p>

        <div class="flex flex-col gap-3 w-full max-w-xs">
          @for (provider of providers; track provider.path) {
            <a
              [href]="provider.path"
              class="flex items-center justify-center gap-3 px-4 py-3 rounded-lg border border-[--color-border] bg-[--color-surface] hover:bg-[--color-surface-elevated] transition-colors text-[--color-text-primary] font-medium"
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
