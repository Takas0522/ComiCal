import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';

import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { VolumeSearchResultsComponent } from '../../organisms/volume-search-results/volume-search-results.component';
import { EmptyStateComponent } from '../../atoms/empty-state/empty-state.component';
import { VolumeApi } from '../../core/api/volume.api';
import { addDaysIso, todayIso } from '../../shared/format/jp-date';
import type { Volume } from '../../core/api/api-types';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [PageLayoutComponent, VolumeSearchResultsComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout heading="ホーム" testid="home">
      <section class="space-y-3" data-testid="home-hero">
        <h2 class="text-3xl font-bold" i18n="@@home.hero.title">まんがリマインダー</h2>
        <p class="text-[var(--color-muted)]" i18n="@@home.hero.tagline">
          あなたの追っているマンガの新刊を、忘れない。
        </p>
      </section>

      <section class="space-y-3" data-testid="home-upcoming">
        <h2 class="text-xl font-semibold" i18n="@@home.upcoming.title">
          今後の発売スケジュール (今後30日)
        </h2>
        <app-volume-search-results
          [items]="upcoming()"
          [loading]="loading()"
          emptyMessage="今後30日以内の発売予定はありません"
          i18n-emptyMessage="@@home.upcoming.empty"
        />
      </section>

      <section class="space-y-3" data-testid="home-popular">
        <h2 class="text-xl font-semibold" i18n="@@home.popular.title">人気の作品</h2>
        <!--
          TODO(phase2+): no popularity API in Phase 1; placeholder until /api/v1/series/popular
          is introduced. See docs/specs/oo-init/03-functional-requirements.md.
        -->
        <app-empty-state
          message="人気作品はまだ集計されていません"
          i18n-message="@@home.popular.empty"
          icon="✨"
          testid="home-popular-empty"
        />
      </section>
    </app-page-layout>
  `,
})
export class HomePage {
  private readonly volumeApi = inject(VolumeApi);

  protected readonly upcoming = signal<readonly Volume[]>([]);
  protected readonly loading = signal<boolean>(true);

  constructor() {
    const from = todayIso();
    const to = addDaysIso(from, 30);
    this.volumeApi
      .searchVolumes({ releaseFrom: from, releaseTo: to, limit: 12 })
      .subscribe({
        next: (r) => {
          this.upcoming.set(r.items);
          this.loading.set(false);
        },
        error: () => {
          // errorInterceptor surfaces the toast; just unblock UI.
          this.loading.set(false);
        },
      });
  }
}
