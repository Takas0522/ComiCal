import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="pointer-events-none fixed inset-x-0 bottom-4 z-50 flex flex-col items-center gap-2 px-4"
      role="status"
      aria-live="polite"
      aria-atomic="true"
      data-testid="toast-container"
    >
      @for (t of toasts(); track t.id) {
        <div
          class="pointer-events-auto w-full max-w-md rounded-md border px-4 py-3 text-sm shadow-md"
          [class.border-red-300]="t.severity === 'error'"
          [class.bg-red-50]="t.severity === 'error'"
          [class.text-red-800]="t.severity === 'error'"
          [class.border-amber-300]="t.severity === 'warning'"
          [class.bg-amber-50]="t.severity === 'warning'"
          [class.text-amber-800]="t.severity === 'warning'"
          [class.border-slate-300]="t.severity === 'info'"
          [class.bg-white]="t.severity === 'info'"
          [class.text-slate-800]="t.severity === 'info'"
          [attr.data-testid]="'toast-' + t.id"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0 flex-1">
              <p class="font-semibold">{{ t.title }}</p>
              @if (t.message) {
                <p class="mt-1 text-xs opacity-90">{{ t.message }}</p>
              }
            </div>
            <button
              type="button"
              class="rounded p-1 text-base leading-none hover:opacity-70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
              i18n-aria-label="@@a11y.toast.dismiss"
              aria-label="通知を閉じる"
              [attr.data-testid]="'toast-dismiss-' + t.id"
              (click)="dismiss(t.id)"
            >×</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class ToastContainerComponent {
  private readonly toastService = inject(ToastService);
  protected readonly toasts = this.toastService.toasts;

  protected dismiss(id: number): void {
    this.toastService.dismiss(id);
  }
}
