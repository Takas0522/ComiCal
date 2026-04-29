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

import { OssDialogService } from '../../core/oss/oss-dialog.service';

@Component({
  selector: 'app-oss-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog
      #dlg
      class="rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] p-0 backdrop:bg-black/50 max-w-2xl w-full"
      data-testid="oss-dialog"
      role="dialog"
      aria-modal="true"
      aria-labelledby="oss-dialog-title"
      (close)="onNativeClose()"
      (keydown)="onKeydown($event)"
    >
      <div class="flex items-center justify-between border-b border-[var(--color-border)] p-4">
        <h2
          id="oss-dialog-title"
          class="text-lg font-bold"
          i18n="@@oss.dialog.heading"
        >
          OSS ライセンス情報
        </h2>
        <button
          type="button"
          class="rounded px-2 py-1 text-sm hover:bg-[var(--color-border)] focus:outline-none focus:ring-2 focus:ring-[var(--color-brand-500)]"
          data-testid="oss-dialog-close"
          i18n-aria-label="@@oss.dialog.close"
          aria-label="閉じる"
          (click)="close()"
        >
          ✕
        </button>
      </div>

      <div class="max-h-[60vh] overflow-y-auto p-4 space-y-3">
        <p class="text-xs text-[var(--color-muted)]" i18n="@@oss.dialog.notice">
          ComiCal は以下の OSS を利用しています。各パッケージのライセンス全文は本リポジトリの
          tools/oss-report/ を参照してください。
        </p>

        @if (svc.loading()) {
          <p data-testid="oss-dialog-loading" i18n="@@oss.dialog.loading">読み込み中…</p>
        } @else if (svc.error()) {
          <p class="text-[var(--color-danger,#dc2626)]" data-testid="oss-dialog-error">
            {{ svc.error() }}
          </p>
        } @else {
          <ul class="divide-y divide-[var(--color-border)]" data-testid="oss-dialog-list">
            @for (pkg of svc.packages() ?? []; track pkg.name + '@' + pkg.version) {
              <li class="py-2 flex flex-wrap items-baseline gap-x-3 text-sm" [attr.data-testid]="'oss-dialog-item-' + pkg.name">
                <a
                  [href]="pkg.url"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="font-medium text-[var(--color-brand-500)] hover:underline"
                >{{ pkg.name }}</a>
                <span class="text-[var(--color-muted)]">{{ pkg.version }}</span>
                <span class="ml-auto rounded bg-[var(--color-border)] px-2 py-0.5 text-xs">{{ pkg.license }}</span>
              </li>
            }
          </ul>
        }
      </div>
    </dialog>
  `,
})
export class OssDialogComponent {
  protected readonly svc = inject(OssDialogService);
  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  private readonly previouslyFocused = signal<HTMLElement | null>(null);

  protected readonly isOpen = computed(() => this.svc.isOpen());

  constructor() {
    effect(() => {
      const open = this.isOpen();
      const el = this.dlg().nativeElement;
      if (open && !el.open) {
        this.previouslyFocused.set(
          (typeof document !== 'undefined' ? (document.activeElement as HTMLElement | null) : null),
        );
        if (typeof el.showModal === 'function') {
          el.showModal();
        } else {
          el.setAttribute('open', '');
        }
        queueMicrotask(() => {
          const closeBtn = el.querySelector<HTMLButtonElement>(
            '[data-testid="oss-dialog-close"]',
          );
          closeBtn?.focus();
        });
      } else if (!open && el.open) {
        if (typeof el.close === 'function') {
          el.close();
        } else {
          el.removeAttribute('open');
        }
      }
    });
  }

  protected close(): void {
    this.svc.close();
    const prev = this.previouslyFocused();
    if (prev && typeof prev.focus === 'function') {
      prev.focus();
    }
  }

  protected onNativeClose(): void {
    if (this.svc.isOpen()) {
      this.svc.close();
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
      return;
    }
    if (event.key === 'Tab') {
      const root = this.dlg().nativeElement;
      const focusables = Array.from(
        root.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"]), input, select, textarea',
        ),
      ).filter((el) => !el.hasAttribute('disabled'));
      if (focusables.length === 0) return;
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const active = document.activeElement as HTMLElement | null;
      if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    }
  }
}
