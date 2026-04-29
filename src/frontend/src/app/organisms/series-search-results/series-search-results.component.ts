import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { SeriesCardComponent } from '../../molecules/series-card/series-card.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { EmptyStateComponent } from '../../atoms/empty-state/empty-state.component';
import type { SeriesSummary } from '../../core/api/api-types';

@Component({
  selector: 'app-series-search-results',
  standalone: true,
  imports: [SeriesCardComponent, SkeletonComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section
      [attr.aria-busy]="loading() ? 'true' : 'false'"
      data-testid="series-search-results"
    >
      @if (loading() && items().length === 0) {
        <ul class="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3" data-testid="series-skeletons">
          @for (i of placeholders; track i) {
            <li><app-skeleton height="6rem" /></li>
          }
        </ul>
      } @else if (items().length === 0) {
        <app-empty-state
          [message]="emptyMessage()"
          icon="🔍"
          testid="series-empty"
        />
      } @else {
        <ul class="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
          @for (s of items(); track s.id) {
            <li><app-series-card [series]="s" /></li>
          }
        </ul>
      }
    </section>
  `,
})
export class SeriesSearchResultsComponent {
  readonly items = input.required<readonly SeriesSummary[]>();
  readonly loading = input<boolean>(false);
  readonly emptyMessage = input<string>('該当するシリーズが見つかりませんでした');

  protected readonly placeholders = [0, 1, 2, 3, 4, 5];
}
