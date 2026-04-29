import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { VolumeCardComponent } from '../../molecules/volume-card/volume-card.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';
import { EmptyStateComponent } from '../../atoms/empty-state/empty-state.component';
import type { Volume } from '../../core/api/api-types';

@Component({
  selector: 'app-volume-search-results',
  standalone: true,
  imports: [VolumeCardComponent, SkeletonComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section
      [attr.aria-busy]="loading() ? 'true' : 'false'"
      data-testid="volume-search-results"
    >
      @if (loading() && items().length === 0) {
        <ul
          class="grid gap-4 grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6"
          data-testid="volume-skeletons"
        >
          @for (i of placeholders; track i) {
            <li>
              <app-skeleton height="14rem" />
            </li>
          }
        </ul>
      } @else if (items().length === 0) {
        <app-empty-state
          [message]="emptyMessage()"
          icon="📚"
          testid="volume-empty"
        />
      } @else {
        <ul class="grid gap-4 grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
          @for (v of items(); track v.id) {
            <li>
              <app-volume-card [volume]="v" [seriesTitle]="seriesTitleOf(v)" />
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class VolumeSearchResultsComponent {
  readonly items = input.required<readonly Volume[]>();
  readonly loading = input<boolean>(false);
  readonly emptyMessage = input<string>('該当する巻が見つかりませんでした');
  /** Optional `seriesId → title` lookup so cards can show series title. */
  readonly seriesTitles = input<Readonly<Record<string, string>>>({});

  protected readonly placeholders = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

  protected seriesTitleOf(v: Volume): string | undefined {
    return this.seriesTitles()[v.seriesId];
  }
}
