import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';

import { AnonymousSubscriptionRepository } from '../../core/anonymous-store';

interface SeriesRef {
  readonly seriesId: string;
  readonly seriesTitle: string;
}

/**
 * Heart-icon toggle for "want-to-read" (読みたい). Stores state into
 * the anonymous IndexedDB; ignored on SSR.
 */
@Component({
  selector: 'app-subscription-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      (click)="toggle($event)"
      [attr.aria-pressed]="isSubscribed()"
      [attr.aria-label]="ariaLabel()"
      [attr.data-testid]="'subscription-toggle'"
      class="inline-flex h-9 w-9 items-center justify-center rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] transition-colors hover:bg-[var(--color-brand-500)]/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
      [class.text-rose-600]="isSubscribed()"
      [class.text-[var(--color-muted)]]="!isSubscribed()"
    >
      <span aria-hidden="true" class="text-lg leading-none">
        {{ isSubscribed() ? '♥' : '♡' }}
      </span>
    </button>
  `,
})
export class SubscriptionToggleComponent {
  readonly series = input.required<SeriesRef>();

  private readonly repo = inject(AnonymousSubscriptionRepository);

  protected readonly isSubscribed = computed(() =>
    this.repo.entries().some((s) => s.seriesId === this.series().seriesId),
  );

  protected readonly ariaLabel = computed(() => {
    const t = this.series().seriesTitle;
    return this.isSubscribed()
      ? `${t} を読みたいから外す`
      : `${t} を読みたいに追加`;
  });

  protected async toggle(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const id = this.series().seriesId;
    if (this.isSubscribed()) {
      await this.repo.remove(id);
    } else {
      await this.repo.add(id);
    }
  }
}
