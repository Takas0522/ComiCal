/// <reference types="@angular/localize" />
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';

import { ButtonComponent } from '../../atoms/button/button.component';
import { MergeService } from '../../core/merge';
import { ToastService } from '../../core/services/toast.service';

/**
 * 匿名→ログイン マージ確認ダイアログ。
 *
 * - 認証直後の自動表示（`AppInitializer` が `MergeService.openPrompt()` を呼ぶ）
 * - 設定ページからの手動表示（同じく `openPrompt()`）
 * - ボタン: [引き継ぐ] [破棄] [後で]
 */
@Component({
  selector: 'app-merge-prompt-dialog',
  standalone: true,
  imports: [ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dlg
      class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-0 backdrop:bg-black/50 max-w-md w-full"
      data-testid="merge-prompt-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="merge-prompt-title"
      (close)="onNativeClose()"
      (keydown)="onKeydown($event)"
    >
      <div class="border-b border-[var(--color-border)] p-4">
        <h2 id="merge-prompt-title" class="text-base font-bold" i18n="@@merge.dialog.title">
          ローカルデータをアカウントに引き継ぎますか？
        </h2>
      </div>
      <div class="p-4 text-sm space-y-2">
        <p data-testid="merge-prompt-body" i18n="@@merge.dialog.body">
          ローカルに保存された
          <strong data-testid="merge-prompt-sub-count">{{ counts().subscriptions }}</strong>
          件の登録漫画と
          <strong data-testid="merge-prompt-purchase-count">{{ counts().purchases }}</strong>
          件の購入履歴をアカウントに引き継ぎますか？
        </p>
        <p class="text-xs text-[var(--color-muted)]" i18n="@@merge.dialog.note">
          「破棄」を選ぶとローカルデータは削除されます。「後で」は 24 時間後に再表示します。
        </p>
      </div>
      <div class="flex flex-wrap justify-end gap-2 border-t border-[var(--color-border)] p-3">
        <app-button
          testid="merge-prompt-snooze"
          i18n-label="@@merge.dialog.snooze"
          label="後で"
          variant="ghost"
          [disabled]="busy()"
          (clicked)="onSnooze()"
        >
          <span i18n="@@merge.dialog.snooze">後で</span>
        </app-button>
        <app-button
          testid="merge-prompt-discard"
          i18n-label="@@merge.dialog.discard"
          label="破棄"
          variant="secondary"
          [disabled]="busy()"
          (clicked)="onDiscard()"
        >
          <span i18n="@@merge.dialog.discard">破棄</span>
        </app-button>
        <app-button
          testid="merge-prompt-merge"
          i18n-label="@@merge.dialog.merge"
          label="引き継ぐ"
          variant="primary"
          [disabled]="busy()"
          [loading]="busy()"
          (clicked)="onMerge()"
        >
          <span i18n="@@merge.dialog.merge">引き継ぐ</span>
        </app-button>
      </div>
    </dialog>
  `,
})
export class MergePromptDialogComponent {
  private readonly merge = inject(MergeService);
  private readonly toast = inject(ToastService);
  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  private readonly previouslyFocused = signal<HTMLElement | null>(null);

  protected readonly busy = computed(() => this.merge.busy());
  protected readonly counts = computed(() => this.merge.pendingCount());
  protected readonly isOpen = computed(() => this.merge.isOpen());

  constructor() {
    effect(() => {
      const open = this.isOpen();
      const el = this.dlg().nativeElement;
      if (open && !el.open) {
        this.previouslyFocused.set(
          (typeof document !== 'undefined'
            ? (document.activeElement as HTMLElement | null)
            : null),
        );
        if (typeof el.showModal === 'function') {
          el.showModal();
        } else {
          el.setAttribute('open', '');
        }
        queueMicrotask(() => {
          el.querySelector<HTMLButtonElement>(
            '[data-testid="merge-prompt-merge"]',
          )?.focus();
        });
      } else if (!open && el.open) {
        if (typeof el.close === 'function') {
          el.close();
        } else {
          el.removeAttribute('open');
        }
        const prev = this.previouslyFocused();
        prev?.focus?.();
      }
    });
  }

  protected onMerge(): void {
    if (this.busy()) return;
    this.merge.merge().subscribe({
      next: (result) => {
        const total = result.merged.subscriptions + result.merged.purchases;
        this.toast.show({
          title: $localize`:@@merge.toast.success:${total}件を引き継ぎました`,
          severity: 'info',
        });
        this.merge.closePrompt();
      },
      error: () => {
        this.toast.show({
          title: $localize`:@@merge.toast.error:引き継ぎに失敗しました`,
          severity: 'error',
        });
      },
    });
  }

  protected async onDiscard(): Promise<void> {
    if (this.busy()) return;
    await this.merge.dismiss();
    this.toast.show({
      title: $localize`:@@merge.toast.discarded:ローカルデータを破棄しました`,
      severity: 'info',
    });
    this.merge.closePrompt();
  }

  protected onSnooze(): void {
    if (this.busy()) return;
    this.merge.snooze();
    this.merge.closePrompt();
  }

  protected onNativeClose(): void {
    if (this.merge.isOpen()) {
      this.merge.closePrompt();
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.onSnooze();
    }
  }
}
