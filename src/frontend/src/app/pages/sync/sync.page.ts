/// <reference types="@angular/localize" />
import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  PLATFORM_ID,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../core/auth';
import { SyncService } from '../../core/sync/sync.service';
import { ToastService } from '../../core/services/toast.service';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

type ViewState = 'idle' | 'redeeming' | 'success' | 'error';

@Component({
  selector: 'app-sync-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="他端末との同期" i18n-heading="@@sync.heading" testid="sync">
      @if (!auth.loaded()) {
        <p
          class="text-sm text-[var(--color-muted)]"
          data-testid="sync-loading"
          i18n="@@sync.auth.loading"
        >
          認証状態を確認しています…
        </p>
      } @else if (!hasToken()) {
        <p
          role="alert"
          class="rounded border border-red-300 bg-red-50 p-3 text-sm text-red-800"
          data-testid="sync-missing-token"
          i18n="@@sync.error.missingToken"
        >
          同期トークンが見つかりません。発行端末で QR コードを表示し、もう一度読み取ってください。
        </p>
      } @else if (!auth.isAuthenticated()) {
        <div class="space-y-3">
          <p
            class="text-sm text-[var(--color-fg)]"
            data-testid="sync-login-required"
            i18n="@@sync.auth.required"
          >
            この端末でログインすると同期が完了します。
          </p>
          <a
            [href]="loginHref()"
            class="inline-flex items-center justify-center rounded-md bg-[var(--color-brand-500)] px-4 py-2 text-sm font-medium text-white shadow hover:bg-[var(--color-brand-700)]"
            data-testid="sync-login-cta"
            rel="nofollow"
            i18n="@@sync.auth.cta"
          >
            ログインして同期を完了
          </a>
        </div>
      } @else {
        @switch (state()) {
          @case ('redeeming') {
            <p
              class="text-sm text-[var(--color-muted)]"
              role="status"
              aria-live="polite"
              data-testid="sync-redeeming"
              i18n="@@sync.redeeming"
            >
              同期しています…
            </p>
          }
          @case ('success') {
            <p
              class="text-sm text-[var(--color-fg)]"
              role="status"
              aria-live="polite"
              data-testid="sync-success"
              i18n="@@sync.success"
            >
              同期が完了しました。ホームに戻ります…
            </p>
          }
          @case ('error') {
            <p
              role="alert"
              class="rounded border border-red-300 bg-red-50 p-3 text-sm text-red-800"
              data-testid="sync-error"
            >
              {{ errorMessage() }}
            </p>
          }
          @default {
            <p
              class="text-sm text-[var(--color-muted)]"
              data-testid="sync-idle"
              i18n="@@sync.idle"
            >
              同期を開始します…
            </p>
          }
        }
      }
    </app-page-layout>
  `,
})
export class SyncPage {
  protected readonly auth = inject(AuthService);
  private readonly sync = inject(SyncService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly token = signal<string | null>(
    this.route.snapshot.queryParamMap.get('token'),
  );

  protected readonly state = signal<ViewState>('idle');
  protected readonly errorMessage = signal<string>('');
  protected readonly hasToken = computed(() => !!this.token());

  protected readonly loginHref = computed(() => {
    const t = this.token() ?? '';
    const returnTo = `/sync?token=${encodeURIComponent(t)}`;
    return this.auth.loginUrl(returnTo);
  });

  private redeemed = false;

  constructor() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    // Defer until auth is loaded; rely on a microtask poll because AuthService
    // exposes `loaded` as a signal and there's no shared effect yet.
    queueMicrotask(() => this.tryRedeem());
  }

  private tryRedeem(): void {
    if (this.redeemed) return;
    if (!this.auth.loaded()) {
      // re-check on the next macrotask until /.auth/me resolves
      setTimeout(() => this.tryRedeem(), 50);
      return;
    }
    if (!this.auth.isAuthenticated() || !this.token()) {
      return;
    }
    this.redeemed = true;
    this.redeemNow();
  }

  private async redeemNow(): Promise<void> {
    const t = this.token();
    if (!t) return;
    this.state.set('redeeming');
    try {
      await this.sync.redeemQrToken(t);
      this.state.set('success');
      this.toast.show({
        title: $localize`:@@sync.toast.success:同期が完了しました`,
        severity: 'info',
      });
      setTimeout(() => {
        void this.router.navigateByUrl('/');
      }, 1200);
    } catch (err) {
      this.state.set('error');
      this.errorMessage.set(this.mapError(err));
    }
  }

  private mapError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const type = (err.error as { type?: string } | null)?.type ?? '';
      if (type.endsWith('sync-token-not-found')) {
        return $localize`:@@sync.error.notFound:同期トークンが見つかりません。発行端末で再度 QR コードを表示してください。`;
      }
      if (type.endsWith('sync-token-expired')) {
        return $localize`:@@sync.error.expired:同期トークンの有効期限が切れています。発行端末で新しい QR を生成してください。`;
      }
      if (type.endsWith('sync-token-already-consumed')) {
        return $localize`:@@sync.error.alreadyConsumed:この同期トークンは既に使用されています。`;
      }
      if (type.endsWith('sync-token-user-mismatch')) {
        return $localize`:@@sync.error.userMismatch:この QR コードは別のアカウントで発行されました。発行端末と同じアカウントでログインしてください。`;
      }
      if (err.status === 401) {
        return $localize`:@@sync.error.unauthorized:認証が必要です。ログインしてもう一度お試しください。`;
      }
    }
    return $localize`:@@sync.error.unknown:同期に失敗しました。時間をおいて再度お試しください。`;
  }
}
