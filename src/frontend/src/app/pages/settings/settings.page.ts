import { isPlatformBrowser } from '@angular/common';
/// <reference types="@angular/localize" />
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { ButtonComponent } from '../../atoms/button/button.component';
import { BadgeComponent } from '../../atoms/badge/badge.component';
import { SettingRowComponent } from '../../molecules/setting-row/setting-row.component';
import {
  ThemeToggleComponent,
  type ThemeMode,
} from '../../molecules/theme-toggle/theme-toggle.component';
import { ThemeService } from '../../core/theme/theme.service';
import {
  AnonymousStoreExportService,
  AnonymousStoreService,
  AnonymousImportSchemaError,
  type AnonymousExport,
} from '../../core/anonymous-store';
import { AuthService } from '../../core/auth/auth.service';
import { MergeService } from '../../core/merge';
import { FeatureFlagService } from '../../core/feature-flags/feature-flag.service';
import {
  FEATURE_FLAG_NAMES,
  type FeatureFlagName,
} from '../../core/feature-flags/feature-flag.types';
import { OssDialogService } from '../../core/oss/oss-dialog.service';
import { ToastService } from '../../core/services/toast.service';
import { AccountService } from '../../core/account/account.service';
import { SyncService, type SyncTokenIssued } from '../../core/sync/sync.service';

interface FlagRow {
  readonly name: FeatureFlagName;
  readonly label: string;
}

const FLAG_LABELS: Record<FeatureFlagName, string> = {
  'qr-sync-enabled': 'QR コード同期',
  'affiliate-link-enabled': 'アフィリエイトリンク',
  'purchase-history-export': '購入履歴エクスポート',
  'dark-mode-system-aware': 'OS テーマに追従',
  'calendar-share-link': 'カレンダー共有リンク',
};

function isAnonymousExport(value: unknown): value is AnonymousExport {
  if (!value || typeof value !== 'object') return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v['schemaVersion'] === 'number' &&
    Array.isArray(v['subscriptions']) &&
    Array.isArray(v['purchases'])
  );
}

function pad2(n: number): string {
  return n < 10 ? `0${n}` : `${n}`;
}

function todayStamp(d: Date = new Date()): string {
  return `${d.getFullYear()}${pad2(d.getMonth() + 1)}${pad2(d.getDate())}`;
}

/** Confirmation phrase the user must type verbatim to enable the delete button. */
const DELETE_CONFIRM_PHRASE = '削除';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    PageLayoutComponent,
    ButtonComponent,
    BadgeComponent,
    SettingRowComponent,
    ThemeToggleComponent,
    RouterLink,
    FormsModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="設定" i18n-heading="@@settings.heading" testid="settings">
      <div class="space-y-6" data-testid="settings-content">
        <!-- a. 表示設定 -->
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
          data-testid="settings-section-display"
          aria-labelledby="settings-section-display-title"
        >
          <h2
            id="settings-section-display-title"
            class="mb-2 text-base font-semibold"
            i18n="@@settings.section.display"
          >
            表示設定
          </h2>
          <app-setting-row
            label="テーマ"
            i18n-label="@@settings.theme.label"
            description="ライト / ダーク / システム連動を選択できます。"
            i18n-description="@@settings.theme.description"
            testidKey="theme"
          >
            <app-theme-toggle
              [value]="theme.theme()"
              (valueChange)="onThemeChange($event)"
            />
          </app-setting-row>
          <app-setting-row
            label="表示言語"
            i18n-label="@@settings.language.label"
            description="現在は日本語のみ提供しています。"
            i18n-description="@@settings.language.description"
            testidKey="language"
          >
            <span
              class="text-sm text-[var(--color-fg)]"
              data-testid="settings-language-value"
              i18n="@@settings.language.value"
            >日本語</span>
          </app-setting-row>
        </section>

        <!-- c. 機能フラグ -->
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
          data-testid="settings-section-flags"
          aria-labelledby="settings-section-flags-title"
        >
          <h2
            id="settings-section-flags-title"
            class="mb-2 text-base font-semibold"
            i18n="@@settings.section.flags"
          >
            機能フラグ
          </h2>
          <p
            class="mb-3 text-xs text-[var(--color-muted)]"
            i18n="@@settings.flags.notice"
          >
            これらの設定はサーバー側で制御されており、ここから変更することはできません。
          </p>
          @for (flag of flagRows; track flag.name) {
            <app-setting-row
              [label]="flag.label"
              [testidKey]="'flag-' + flag.name"
            >
              @if (isFlagEnabled(flag.name)()) {
                <app-badge tone="success" [testid]="'settings-flag-' + flag.name + '-on'">
                  <span i18n="@@settings.flag.on">有効</span>
                </app-badge>
              } @else {
                <app-badge tone="neutral" [testid]="'settings-flag-' + flag.name + '-off'">
                  <span i18n="@@settings.flag.off">無効</span>
                </app-badge>
              }
            </app-setting-row>
          }
        </section>

        <!-- d. ローカルデータ -->
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
          data-testid="settings-section-local"
          aria-labelledby="settings-section-local-title"
        >
          <h2
            id="settings-section-local-title"
            class="mb-2 text-base font-semibold"
            i18n="@@settings.section.local"
          >
            ローカルデータ
          </h2>
          <app-setting-row
            label="保存件数"
            i18n-label="@@settings.local.count.label"
            description="読みたい / 購入の合計件数（端末内に保存）。"
            i18n-description="@@settings.local.count.description"
            testidKey="local-count"
          >
            <span
              class="text-sm font-medium"
              data-testid="settings-local-count"
            >{{ totalEntries() }}</span>
          </app-setting-row>

          <app-setting-row
            label="エクスポート"
            i18n-label="@@settings.local.export.label"
            description="ローカルデータを JSON ファイルとしてダウンロードします。"
            i18n-description="@@settings.local.export.description"
            testidKey="local-export"
          >
            <app-button
              testid="settings-export"
              i18n-label="@@settings.local.export.button"
              label="エクスポート"
              variant="secondary"
              [disabled]="exporting()"
              (clicked)="onExport()"
            >
              <span i18n="@@settings.local.export.button">エクスポート</span>
            </app-button>
          </app-setting-row>

          <app-setting-row
            label="インポート"
            i18n-label="@@settings.local.import.label"
            description="エクスポートした JSON ファイルを読み込んで結合します。"
            i18n-description="@@settings.local.import.description"
            testidKey="local-import"
          >
            <label
              class="inline-flex cursor-pointer items-center rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] px-4 py-2 text-sm font-medium hover:bg-[var(--color-border)]"
              data-testid="settings-import-label"
            >
              <span i18n="@@settings.local.import.button">インポート</span>
              <input
                #importInput
                type="file"
                accept="application/json"
                class="sr-only"
                data-testid="settings-import"
                (change)="onImport($event)"
              />
            </label>
          </app-setting-row>

          <app-setting-row
            label="ローカルデータをクリア"
            i18n-label="@@settings.local.clear.label"
            description="読みたい / 購入の記録をすべて削除します（取り消しできません）。"
            i18n-description="@@settings.local.clear.description"
            testidKey="local-clear"
          >
            <app-button
              testid="settings-clear"
              i18n-label="@@settings.local.clear.button"
              label="ローカルデータをクリア"
              variant="secondary"
              (clicked)="onClearRequest()"
            >
              <span i18n="@@settings.local.clear.button">ローカルデータをクリア</span>
            </app-button>
          </app-setting-row>

          @if (showMergeRow()) {
            <app-setting-row
              label="ローカルデータを引き継ぐ"
              i18n-label="@@settings.local.merge.label"
              description="ローカルに保存された「読みたい」「購入」をログイン中のアカウントに統合します。"
              i18n-description="@@settings.local.merge.description"
              testidKey="local-merge"
            >
              <app-button
                testid="settings-merge"
                i18n-label="@@settings.local.merge.button"
                label="アカウントに引き継ぐ"
                variant="primary"
                (clicked)="onOpenMerge()"
              >
                <span i18n="@@settings.local.merge.button">アカウントに引き継ぐ</span>
              </app-button>
            </app-setting-row>
          }
        </section>

        <!-- e. OSS / ライセンス -->
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
          data-testid="settings-section-oss"
          aria-labelledby="settings-section-oss-title"
        >
          <h2
            id="settings-section-oss-title"
            class="mb-2 text-base font-semibold"
            i18n="@@settings.section.oss"
          >
            OSS / ライセンス
          </h2>
          <app-setting-row
            label="利用している OSS"
            i18n-label="@@settings.oss.label"
            description="本アプリが利用するオープンソースソフトウェアの一覧を表示します。"
            i18n-description="@@settings.oss.description"
            testidKey="oss"
          >
            <app-button
              testid="settings-oss-open"
              i18n-label="@@settings.oss.button"
              label="OSS 情報を表示"
              variant="secondary"
              (clicked)="onOpenOss()"
            >
              <span i18n="@@settings.oss.button">OSS 情報を表示</span>
            </app-button>
          </app-setting-row>
        </section>

        <!-- e2. 他端末との同期 -->
        @if (isAuthenticated()) {
          <section
            class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
            data-testid="settings-section-sync"
            aria-labelledby="settings-section-sync-title"
          >
            <h2
              id="settings-section-sync-title"
              class="mb-2 text-base font-semibold"
              i18n="@@settings.section.sync"
            >
              他端末との同期
            </h2>
            <app-setting-row
              label="QR コードを表示"
              i18n-label="@@settings.sync.label"
              description="別の端末でこの QR を読み取ると、同じアカウントへの同期が完了します（5 分間有効）。"
              i18n-description="@@settings.sync.description"
              testidKey="sync"
            >
              <app-button
                testid="settings-sync-issue"
                i18n-label="@@settings.sync.button"
                label="QR コードを表示"
                variant="secondary"
                [disabled]="syncIssuing()"
                (clicked)="onIssueSyncToken()"
              >
                <span i18n="@@settings.sync.button">QR コードを表示</span>
              </app-button>
            </app-setting-row>
          </section>
        }

        <!-- f. アカウント -->
        <section
          class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-4"
          data-testid="settings-section-account"
          aria-labelledby="settings-section-account-title"
        >
          <h2
            id="settings-section-account-title"
            class="mb-2 text-base font-semibold"
            i18n="@@settings.section.account"
          >
            アカウント
          </h2>
          @if (isAuthenticated()) {
            <!-- Danger zone: hard-deletes the account per 個人情報保護法. -->
            <div
              class="mt-2 rounded-[var(--radius-card)] border border-red-500/40 bg-red-500/5 p-4"
              data-testid="settings-account-delete-section"
              aria-labelledby="settings-account-delete-title"
            >
              <button
                type="button"
                class="flex w-full items-center justify-between text-left text-sm font-semibold text-red-600 hover:text-red-700"
                data-testid="settings-account-delete-toggle"
                [attr.aria-expanded]="deleteExpanded()"
                aria-controls="settings-account-delete-body"
                (click)="onToggleDeleteSection()"
              >
                <span id="settings-account-delete-title" i18n="@@settings.account.deleteSection.title">
                  アカウントを削除
                </span>
                <span aria-hidden="true">{{ deleteExpanded() ? '−' : '+' }}</span>
              </button>
              @if (deleteExpanded()) {
                <div
                  id="settings-account-delete-body"
                  class="mt-3 space-y-3 text-sm"
                  data-testid="settings-account-delete-body"
                >
                  <p
                    class="text-[var(--color-fg)]"
                    i18n="@@settings.account.deleteSection.warning"
                  >
                    アカウントとすべての登録漫画・購入履歴が完全に削除されます。この操作は取り消せません。
                  </p>
                  <label class="block">
                    <span
                      class="mb-1 block text-xs text-[var(--color-muted)]"
                      i18n="@@settings.account.deleteSection.confirmInputLabel"
                    >
                      確認のため「削除」と入力してください
                    </span>
                    <input
                      type="text"
                      autocomplete="off"
                      [ngModel]="deleteConfirmInput()"
                      (ngModelChange)="deleteConfirmInput.set($event)"
                      class="w-full rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm text-[var(--color-fg)] focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-500/40"
                      data-testid="settings-account-delete-confirm-input"
                      [attr.aria-label]="confirmInputAriaLabel"
                    />
                  </label>
                  <app-button
                    testid="settings-account-delete-button"
                    i18n-label="@@settings.account.deleteSection.button"
                    label="アカウントを削除する"
                    variant="primary"
                    [disabled]="!canDelete()"
                    [loading]="deleting()"
                    (clicked)="onDeleteRequest()"
                  >
                    <span i18n="@@settings.account.deleteSection.button">アカウントを削除する</span>
                  </app-button>
                </div>
              }
            </div>
          } @else {
            <p
              class="text-sm text-[var(--color-muted)]"
              data-testid="settings-account-anon"
            >
              <a
                routerLink="/login"
                class="text-[var(--color-brand-500)] hover:underline"
                data-testid="settings-login-link"
                i18n="@@settings.account.login"
              >ログインしてアカウント設定を有効化</a>
            </p>
          }
        </section>
      </div>

      <!-- Confirm clear dialog -->
      <dialog
        #confirmDlg
        class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-0 backdrop:bg-black/50 max-w-md w-full"
        data-testid="settings-clear-confirm"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="settings-clear-confirm-title"
      >
        <div class="border-b border-[var(--color-border)] p-4">
          <h2 id="settings-clear-confirm-title" class="text-base font-bold" i18n="@@settings.local.clear.confirm.title">
            ローカルデータをクリアしますか？
          </h2>
        </div>
        <div class="p-4 text-sm">
          <p i18n="@@settings.local.clear.confirm.body">
            「読みたい」「購入」の記録がすべて削除されます。この操作は取り消せません。
          </p>
        </div>
        <div class="flex justify-end gap-2 border-t border-[var(--color-border)] p-3">
          <app-button
            testid="settings-clear-cancel"
            i18n-label="@@settings.local.clear.confirm.cancel"
            label="キャンセル"
            variant="ghost"
            (clicked)="onClearCancel()"
          >
            <span i18n="@@settings.local.clear.confirm.cancel">キャンセル</span>
          </app-button>
          <app-button
            testid="settings-clear-confirm-button"
            i18n-label="@@settings.local.clear.confirm.ok"
            label="クリアする"
            variant="primary"
            (clicked)="onClearConfirm()"
          >
            <span i18n="@@settings.local.clear.confirm.ok">クリアする</span>
          </app-button>
        </div>
      </dialog>

      <!-- Confirm account deletion dialog -->
      <dialog
        #deleteDlg
        class="rounded-[var(--radius-card)] border border-red-500/40 bg-[var(--color-surface)] p-0 backdrop:bg-black/50 max-w-md w-full"
        data-testid="settings-account-delete-confirm"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="settings-account-delete-confirm-title"
      >
        <div class="border-b border-[var(--color-border)] p-4">
          <h2
            id="settings-account-delete-confirm-title"
            class="text-base font-bold text-red-600"
            i18n="@@settings.account.deleteSection.confirm.title"
          >
            本当にアカウントを削除しますか？
          </h2>
        </div>
        <div class="p-4 text-sm">
          <p i18n="@@settings.account.deleteSection.confirm.body">
            この操作は取り消せません。すべてのデータが完全に削除され、削除後はログアウトされます。
          </p>
        </div>
        <div class="flex justify-end gap-2 border-t border-[var(--color-border)] p-3">
          <app-button
            testid="settings-account-delete-cancel"
            i18n-label="@@settings.account.deleteSection.confirm.cancel"
            label="キャンセル"
            variant="ghost"
            (clicked)="onDeleteCancel()"
          >
            <span i18n="@@settings.account.deleteSection.confirm.cancel">キャンセル</span>
          </app-button>
          <app-button
            testid="settings-account-delete-confirm-button"
            i18n-label="@@settings.account.deleteSection.confirm.ok"
            label="削除する"
            variant="primary"
            [loading]="deleting()"
            [disabled]="deleting()"
            (clicked)="onDeleteConfirm()"
          >
            <span i18n="@@settings.account.deleteSection.confirm.ok">削除する</span>
          </app-button>
        </div>
      </dialog>

      <!-- QR Sync dialog -->
      <dialog
        #syncDlg
        class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-0 backdrop:bg-black/50 max-w-md w-full"
        data-testid="settings-sync-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="settings-sync-dialog-title"
      >
        <div class="border-b border-[var(--color-border)] p-4">
          <h2 id="settings-sync-dialog-title" class="text-base font-bold" i18n="@@settings.sync.dialog.title">
            QR コードで同期
          </h2>
        </div>
        <div class="space-y-3 p-4 text-sm">
          @if (syncIssued(); as issued) {
            <p i18n="@@settings.sync.dialog.body">
              別の端末で以下の QR を読み取ると、同じアカウントへの同期画面が開きます。
            </p>
            <div class="flex justify-center">
              <img
                [src]="syncQrDataUrl()"
                alt="同期用 QR コード"
                i18n-alt="@@settings.sync.dialog.qrAlt"
                width="192"
                height="192"
                decoding="async"
                class="h-48 w-48 rounded bg-white p-2"
                data-testid="settings-sync-qr-image"
              />
            </div>
            <div>
              <label
                for="settings-sync-token-text"
                class="mb-1 block text-xs text-[var(--color-muted)]"
                i18n="@@settings.sync.dialog.tokenLabel"
              >
                トークン（コピーして手入力でも可）
              </label>
              <div class="flex gap-2">
                <input
                  id="settings-sync-token-text"
                  type="text"
                  readonly
                  [value]="issued.token"
                  data-testid="settings-sync-token"
                  class="flex-1 rounded border border-[var(--color-border)] bg-[var(--color-bg)] px-2 py-1 font-mono text-xs"
                />
                <app-button
                  testid="settings-sync-copy"
                  i18n-label="@@settings.sync.dialog.copy"
                  label="コピー"
                  variant="ghost"
                  (clicked)="onCopySyncToken()"
                >
                  <span i18n="@@settings.sync.dialog.copy">コピー</span>
                </app-button>
              </div>
            </div>
            <p
              class="text-xs text-[var(--color-muted)]"
              role="status"
              aria-live="polite"
              data-testid="settings-sync-countdown"
            >
              <ng-container i18n="@@settings.sync.dialog.expiresIn">残り</ng-container>
              {{ syncRemainingSeconds() }}
              <ng-container i18n="@@settings.sync.dialog.seconds">秒</ng-container>
            </p>
          } @else {
            <p
              class="text-sm text-[var(--color-muted)]"
              role="status"
              data-testid="settings-sync-loading"
              i18n="@@settings.sync.dialog.loading"
            >
              QR コードを生成しています…
            </p>
          }
        </div>
        <div class="flex justify-end gap-2 border-t border-[var(--color-border)] p-3">
          <app-button
            testid="settings-sync-close"
            i18n-label="@@settings.sync.dialog.close"
            label="閉じる"
            variant="ghost"
            (clicked)="onCloseSyncDialog()"
          >
            <span i18n="@@settings.sync.dialog.close">閉じる</span>
          </app-button>
        </div>
      </dialog>
    </app-page-layout>
  `,
})
export class SettingsPage {
  protected readonly theme = inject(ThemeService);
  private readonly store = inject(AnonymousStoreService);
  private readonly exporter = inject(AnonymousStoreExportService);
  private readonly flags = inject(FeatureFlagService);
  private readonly oss = inject(OssDialogService);
  private readonly toast = inject(ToastService);
  private readonly account = inject(AccountService);
  private readonly auth = inject(AuthService);
  private readonly merge = inject(MergeService);
  private readonly platformId = inject(PLATFORM_ID);

  protected readonly totalEntries = computed(() => this.store.totalLocalEntries());
  protected readonly exporting = signal(false);

  protected readonly showMergeRow = computed(
    () => this.auth.isAuthenticated() && this.store.totalLocalEntries() > 0,
  );

  protected readonly flagRows: readonly FlagRow[] = FEATURE_FLAG_NAMES.map((name) => ({
    name,
    label: FLAG_LABELS[name],
  }));

  // Account deletion danger zone state. The expand-to-show pattern is used so
  // the destructive action stays out of sight until the user opts in.
  protected readonly isAuthenticated = computed(() => this.auth.isAuthenticated());
  protected readonly deleteExpanded = signal(false);
  protected readonly deleteConfirmInput = signal('');
  protected readonly deleting = signal(false);
  protected readonly canDelete = computed(
    () => this.deleteConfirmInput().trim() === DELETE_CONFIRM_PHRASE && !this.deleting(),
  );
  protected readonly confirmInputAriaLabel = $localize`:@@settings.account.deleteSection.confirmInputAriaLabel:確認のため「削除」と入力してください`;

  private readonly confirmDlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('confirmDlg');
  private readonly importInput =
    viewChild.required<ElementRef<HTMLInputElement>>('importInput');
  private readonly deleteDlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('deleteDlg');
  private readonly syncDlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('syncDlg');

  private readonly sync = inject(SyncService);
  protected readonly syncIssuing = signal(false);
  protected readonly syncIssued = signal<SyncTokenIssued | null>(null);
  protected readonly syncQrDataUrl = signal<string>('');
  protected readonly syncRemainingSeconds = signal<number>(0);
  private syncCountdownHandle: ReturnType<typeof setInterval> | null = null;

  protected isFlagEnabled(name: FeatureFlagName) {
    return this.flags.isEnabled(name);
  }

  protected onThemeChange(mode: ThemeMode): void {
    this.theme.setTheme(mode);
  }

  protected async onExport(): Promise<void> {
    if (this.exporting()) return;
    this.exporting.set(true);
    try {
      const data = await this.exporter.exportAll();
      const json = JSON.stringify(data, null, 2);
      if (typeof window === 'undefined' || typeof document === 'undefined') return;
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `comical-anon-export-${todayStamp()}.json`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      this.toast.show({
        title: $localize`:@@settings.toast.export.title:エクスポートが完了しました`,
        severity: 'info',
      });
    } catch {
      this.toast.show({
        title: $localize`:@@settings.toast.export.error:エクスポートに失敗しました`,
        severity: 'error',
      });
    } finally {
      this.exporting.set(false);
    }
  }

  protected async onImport(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const parsed: unknown = JSON.parse(text);
      if (!isAnonymousExport(parsed)) {
        throw new AnonymousImportSchemaError(
          (parsed as { schemaVersion?: unknown })?.schemaVersion,
        );
      }
      await this.exporter.importAll(parsed);
      this.toast.show({
        title: $localize`:@@settings.toast.import.title:インポートが完了しました`,
        severity: 'info',
      });
    } catch {
      this.toast.show({
        title: $localize`:@@settings.toast.import.error:インポートに失敗しました`,
        message: $localize`:@@settings.toast.import.error.detail:ファイルの形式を確認してください。`,
        severity: 'error',
      });
    } finally {
      input.value = '';
    }
  }

  protected onClearRequest(): void {
    const el = this.confirmDlg().nativeElement;
    if (typeof el.showModal === 'function') {
      el.showModal();
    } else {
      el.setAttribute('open', '');
    }
  }

  protected onClearCancel(): void {
    const el = this.confirmDlg().nativeElement;
    if (typeof el.close === 'function') {
      el.close();
    } else {
      el.removeAttribute('open');
    }
  }

  protected async onClearConfirm(): Promise<void> {
    try {
      await Promise.all([
        this.store.subscriptions.clear(),
        this.store.purchases.clear(),
      ]);
      this.toast.show({
        title: $localize`:@@settings.toast.clear.title:ローカルデータを削除しました`,
        severity: 'info',
      });
    } catch {
      this.toast.show({
        title: $localize`:@@settings.toast.clear.error:削除に失敗しました`,
        severity: 'error',
      });
    } finally {
      this.onClearCancel();
    }
  }

  protected async onOpenOss(): Promise<void> {
    await this.oss.open();
  }

  protected onOpenMerge(): void {
    this.merge.openPrompt();
  }

  protected async onIssueSyncToken(): Promise<void> {
    if (this.syncIssuing()) return;
    this.syncIssuing.set(true);
    try {
      const issued = await this.sync.issueQrToken();
      this.syncIssued.set(issued);
      // Lazy-load qrcode only when the user actually opens the QR sync flow.
      // Saves ~30KB from the settings-page chunk for users who never use this feature.
      const QRCode = await import('qrcode');
      const dataUrl = await QRCode.toDataURL(issued.qrPayload, {
        errorCorrectionLevel: 'M',
        margin: 1,
        width: 320,
      });
      this.syncQrDataUrl.set(dataUrl);
      this.startSyncCountdown(issued.expiresAt);
      this.openSyncDialog();
    } catch {
      this.toast.show({
        title: $localize`:@@settings.toast.sync.error:QR コードの生成に失敗しました`,
        severity: 'error',
      });
    } finally {
      this.syncIssuing.set(false);
    }
  }

  protected async onCopySyncToken(): Promise<void> {
    const issued = this.syncIssued();
    if (!issued) return;
    try {
      if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(issued.token);
      }
      this.toast.show({
        title: $localize`:@@settings.toast.sync.copied:コピーしました`,
        severity: 'info',
      });
    } catch {
      this.toast.show({
        title: $localize`:@@settings.toast.sync.copyError:コピーに失敗しました`,
        severity: 'error',
      });
    }
  }

  protected onCloseSyncDialog(): void {
    this.stopSyncCountdown();
    this.syncIssued.set(null);
    this.syncQrDataUrl.set('');
    const el = this.syncDlg().nativeElement;
    if (typeof el.close === 'function') {
      el.close();
    } else {
      el.removeAttribute('open');
    }
  }

  private openSyncDialog(): void {
    const el = this.syncDlg().nativeElement;
    if (typeof el.showModal === 'function') {
      el.showModal();
    } else {
      el.setAttribute('open', '');
    }
  }

  private startSyncCountdown(expiresAtIso: string): void {
    this.stopSyncCountdown();
    const expiresAtMs = Date.parse(expiresAtIso);
    if (Number.isNaN(expiresAtMs)) return;
    const tick = () => {
      const remaining = Math.max(0, Math.floor((expiresAtMs - Date.now()) / 1000));
      this.syncRemainingSeconds.set(remaining);
      if (remaining <= 0) {
        this.stopSyncCountdown();
        this.onCloseSyncDialog();
      }
    };
    tick();
    if (typeof setInterval === 'function') {
      this.syncCountdownHandle = setInterval(tick, 1000);
    }
  }

  private stopSyncCountdown(): void {
    if (this.syncCountdownHandle !== null) {
      clearInterval(this.syncCountdownHandle);
      this.syncCountdownHandle = null;
    }
  }

  protected onToggleDeleteSection(): void {
    this.deleteExpanded.update((open) => !open);
    if (!this.deleteExpanded()) {
      this.deleteConfirmInput.set('');
    }
  }

  protected onDeleteRequest(): void {
    if (!this.canDelete()) return;
    const el = this.deleteDlg().nativeElement;
    if (typeof el.showModal === 'function') {
      el.showModal();
    } else {
      el.setAttribute('open', '');
    }
  }

  protected onDeleteCancel(): void {
    const el = this.deleteDlg().nativeElement;
    if (typeof el.close === 'function') {
      el.close();
    } else {
      el.removeAttribute('open');
    }
  }

  protected onDeleteConfirm(): void {
    if (this.deleting()) return;
    this.deleting.set(true);
    this.account.deleteAccount().subscribe({
      next: () => {
        this.onDeleteCancel();
        this.toast.show({
          title: $localize`:@@settings.account.deleteSection.toast.success:アカウントを削除しました`,
          severity: 'info',
        });
        // Force the SWA logout round-trip — the cookie may still be valid
        // server-side for a few minutes but the principal no longer maps.
        if (isPlatformBrowser(this.platformId)) {
          window.location.assign(this.auth.logoutUrl('/'));
        }
      },
      error: () => {
        this.deleting.set(false);
        this.toast.show({
          title: $localize`:@@settings.account.deleteSection.toast.error:アカウントの削除に失敗しました`,
          message: $localize`:@@settings.account.deleteSection.toast.error.detail:時間をおいて再度お試しください。`,
          severity: 'error',
        });
      },
    });
  }
}
