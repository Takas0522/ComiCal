import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';

import { AnonymousPurchaseRepository } from '../../core/anonymous-store';
import type { PurchaseState } from '../../core/anonymous-store';

interface VolumeRef {
  readonly volumeId: string;
  readonly seriesId: string;
  readonly isbn13: string;
}

type CycleState = 'none' | PurchaseState;

const CYCLE: readonly CycleState[] = ['none', 'bought', 'finished'];

const LABELS: Record<CycleState, string> = {
  none: '未',
  bought: '買った',
  reading: '読書中',
  finished: '読了',
};

/**
 * 3-state cycle button: 未 → 買った → 読了 → 未.
 * (`reading` is supported by the data model but not part of the cycle UI.)
 */
@Component({
  selector: 'app-purchase-state-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      (click)="cycle($event)"
      [attr.data-testid]="'purchase-state-toggle'"
      [attr.data-state]="state()"
      [attr.aria-label]="ariaLabel()"
      class="inline-flex h-9 min-w-[3.5rem] items-center justify-center rounded-full border border-[var(--color-border)] bg-[var(--color-surface)] px-3 text-xs font-medium transition-colors hover:bg-[var(--color-brand-500)]/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
      [class.text-emerald-700]="state() === 'finished'"
      [class.text-[var(--color-brand-700)]]="state() === 'bought'"
      [class.text-[var(--color-muted)]]="state() === 'none'"
    >
      {{ label() }}
    </button>
  `,
})
export class PurchaseStateToggleComponent {
  readonly volume = input.required<VolumeRef>();

  private readonly repo = inject(AnonymousPurchaseRepository);

  protected readonly state = computed<CycleState>(() => {
    const id = this.volume().volumeId;
    const found = this.repo.entries().find((p) => p.volumeId === id);
    return found ? found.state : 'none';
  });

  protected readonly label = computed(() => LABELS[this.state()]);

  protected readonly ariaLabel = computed(
    () => `購入状態: ${this.label()} (クリックで変更)`,
  );

  protected async cycle(event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const cur = this.state();
    const idx = CYCLE.indexOf(cur);
    const next = CYCLE[(idx + 1) % CYCLE.length];
    const v = this.volume();
    if (next === 'none') {
      await this.repo.remove(v.volumeId);
      return;
    }
    const now = new Date().toISOString();
    await this.repo.upsert({
      volumeId: v.volumeId,
      seriesId: v.seriesId,
      isbn13: v.isbn13,
      state: next,
      updatedAt: now,
      ...(next === 'bought' ? { purchasedAt: now } : {}),
    });
  }
}
