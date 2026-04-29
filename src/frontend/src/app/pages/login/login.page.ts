import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { AuthService } from '../../core/auth';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="ログイン" testid="login">
      <p
        class="text-sm text-[var(--color-muted)]"
        data-testid="login-lede"
        i18n="@@login.lede"
      >
        ログインしてマンガリストを同期
      </p>

      @if (errorMessage(); as msg) {
        <div
          role="alert"
          class="rounded border border-red-300 bg-red-50 p-3 text-sm text-red-800"
          data-testid="login-error"
          i18n="@@login.error"
        >
          {{ msg }}
        </div>
      }

      <div class="mt-4 flex flex-col gap-2">
        <a
          [href]="loginHref()"
          class="inline-flex items-center justify-center rounded-md bg-[var(--color-brand-500)] px-4 py-2 text-sm font-medium text-white shadow hover:bg-[var(--color-brand-700)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
          data-testid="login-aadb2c"
          rel="nofollow"
          i18n="@@login.cta.aadb2c"
        >
          ログイン (Entra External ID)
        </a>
        <p class="text-xs text-[var(--color-muted)]" i18n="@@login.help">
          ログインすると Microsoft / Google / X (Twitter) アカウントが利用できます。
        </p>
      </div>
    </app-page-layout>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly returnTo = signal<string>(this.route.snapshot.queryParamMap.get('returnTo') ?? '/');

  protected readonly loginHref = computed(() => this.auth.loginUrl(this.returnTo()));

  protected readonly errorMessage = computed<string | null>(() => {
    const code = this.route.snapshot.queryParamMap.get('error');
    if (!code) {
      return null;
    }
    return `ログインに失敗しました (${code})。もう一度お試しください。`;
  });
}
