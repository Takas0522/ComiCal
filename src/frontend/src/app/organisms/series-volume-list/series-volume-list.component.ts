import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { VolumeCardComponent } from '../../molecules/volume-card/volume-card.component';
import { EmptyStateComponent } from '../../atoms/empty-state/empty-state.component';
import { formatJpDate, isoYearMonth } from '../../shared/format/jp-date';
import type { Volume } from '../../core/api/api-types';

interface MonthGroup {
  readonly key: string;
  readonly label: string;
  readonly volumes: readonly Volume[];
}

@Component({
  selector: 'app-series-volume-list',
  standalone: true,
  imports: [VolumeCardComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (groups().length === 0) {
      <app-empty-state
        message="登録されている巻はまだありません"
        icon="📚"
        testid="series-volume-list-empty"
      />
    } @else {
      <div class="space-y-6" data-testid="series-volume-list">
        @for (group of groups(); track group.key) {
          <section>
            <h3 class="text-lg font-semibold mb-3" data-testid="series-volume-list-month">
              {{ group.label }}
            </h3>
            <ul class="grid gap-4 grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
              @for (v of group.volumes; track v.id) {
                <li>
                  <app-volume-card [volume]="v" [seriesTitle]="seriesTitle()" />
                </li>
              }
            </ul>
          </section>
        }
      </div>
    }
  `,
})
export class SeriesVolumeListComponent {
  readonly volumes = input.required<readonly Volume[]>();
  readonly seriesTitle = input<string | undefined>(undefined);

  protected readonly groups = computed<readonly MonthGroup[]>(() => {
    const byMonth = new Map<string, Volume[]>();
    for (const v of this.volumes()) {
      const key = isoYearMonth(v.releaseDate);
      const arr = byMonth.get(key) ?? [];
      arr.push(v);
      byMonth.set(key, arr);
    }
    return Array.from(byMonth.entries())
      .sort(([a], [b]) => (a < b ? 1 : a > b ? -1 : 0)) // descending: future first
      .map(([key, vols]) => ({
        key,
        label: key === '0000-00' ? '発売日未定' : formatJpDate(`${key}-01`, true),
        volumes: vols.slice().sort((a, b) => {
          const da = a.releaseDate ?? '';
          const db = b.releaseDate ?? '';
          return da < db ? 1 : da > db ? -1 : 0;
        }),
      }));
  });
}
