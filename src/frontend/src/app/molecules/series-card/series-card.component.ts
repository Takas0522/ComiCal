import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BadgeComponent } from '../../atoms/badge/badge.component';
import type { SeriesSummary } from '../../core/api/api-types';
import { SubscriptionToggleComponent } from '../subscription-toggle/subscription-toggle.component';

@Component({
  selector: 'app-series-card',
  standalone: true,
  imports: [RouterLink, BadgeComponent, SubscriptionToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="relative rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] transition-shadow hover:shadow-md focus-within:ring-2 focus-within:ring-[var(--color-brand-500)]"
    >
      <a
        [routerLink]="['/series', series().id]"
        class="block p-4 pr-14 focus-visible:outline-none"
        data-testid="series-card"
        [attr.aria-label]="series().title"
      >
        <h3 class="text-base font-semibold line-clamp-2" data-testid="series-card-title">
          {{ series().title }}
        </h3>
        <div class="mt-2 flex items-center gap-2">
          @if (statusLabel(); as label) {
            <app-badge
              [tone]="series().isCompleted ? 'success' : 'brand'"
              testid="series-card-status"
            >{{ label }}</app-badge>
          }
        </div>
      </a>
      <div class="absolute right-3 top-3">
        <app-subscription-toggle [series]="toggleRef()" />
      </div>
    </div>
  `,
})
export class SeriesCardComponent {
  readonly series = input.required<SeriesSummary>();

  protected readonly statusLabel = computed(() => (this.series().isCompleted ? '完結' : '連載中'));

  protected readonly toggleRef = computed(() => ({
    seriesId: this.series().id,
    seriesTitle: this.series().title,
  }));
}
