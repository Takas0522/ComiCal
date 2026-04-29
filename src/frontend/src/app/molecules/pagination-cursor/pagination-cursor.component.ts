import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination-cursor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (hasMore()) {
      <div class="flex justify-center py-4">
        <button
          type="button"
          (click)="loadMore.emit()"
          [disabled]="loading()"
          class="rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-6 py-2 text-sm font-medium hover:bg-[var(--color-border)] disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
          data-testid="pagination-load-more"
        >
          @if (loading()) {
            <span i18n="@@pagination.loading">読み込み中…</span>
          } @else {
            <span i18n="@@pagination.loadMore">もっと見る</span>
          }
        </button>
      </div>
    }
  `,
})
export class PaginationCursorComponent {
  readonly nextCursor = input<string | null | undefined>(null);
  readonly loading = input<boolean>(false);
  readonly loadMore = output<void>();

  protected readonly hasMore = computed(() => {
    const c = this.nextCursor();
    return typeof c === 'string' && c.length > 0;
  });
}
