import { afterNextRender, ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { KeywordFilterComponent, KeywordUpdate } from '../../molecules/keyword-filter';
import {
  KeywordMutationResult,
  MAX_KEYWORDS,
  UpcomingFilterStore,
} from '../../features/upcoming-filter.store';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-keywords-settings-page',
  standalone: true,
  imports: [KeywordFilterComponent, PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-keywords-settings" class="max-w-lg py-5">
        <h1 class="mb-2 text-xl font-bold text-[--color-text-primary]" i18n>絞り込みキーワード</h1>
        <p class="mb-5 text-sm text-[--color-text-secondary]" i18n>
          登録したキーワードでホームとカレンダーの発売予定を自動的に絞り込みます。
        </p>

        <app-keyword-filter
          [keywords]="store.keywords()"
          [status]="status()"
          (add)="addKeyword($event)"
          (update)="updateKeyword($event)"
          (remove)="removeKeyword($event)"
        />

        @if (store.restored() && store.keywords().length === 0) {
          <p
            data-testid="keywords-settings-empty-state"
            class="mt-5 rounded-xl bg-[--color-surface] p-4 text-sm text-[--color-text-secondary]"
            i18n
          >
            キーワードを登録すると、ホームとカレンダーの発売予定を自動で絞り込めます。
          </p>
        }
      </div>
    </app-page-layout>
  `,
})
export class KeywordsSettingsPage {
  protected readonly store = inject(UpcomingFilterStore);
  protected readonly status = signal<string | null>(null);

  constructor() {
    afterNextRender(() => void this.store.restore());
  }

  async addKeyword(keyword: string) {
    this.status.set(this.messageFor(await this.store.addKeyword(keyword), '追加しました。'));
  }

  async updateKeyword(update: KeywordUpdate) {
    this.status.set(
      this.messageFor(
        await this.store.updateKeyword(update.index, update.keyword),
        '更新しました。',
      ),
    );
  }

  async removeKeyword(index: number) {
    this.status.set(this.messageFor(await this.store.removeKeyword(index), '削除しました。'));
  }

  private messageFor(result: KeywordMutationResult, successMessage: string): string {
    if (result.success) return `絞り込みキーワードを${successMessage}`;

    switch (result.reason) {
      case 'empty':
        return 'キーワードを入力してください。';
      case 'duplicate':
        return '同じキーワードは登録できません。';
      case 'too-many-keywords':
        return `キーワードは${MAX_KEYWORDS}件まで登録できます。`;
      case 'too-long':
        return 'キーワードの合計は512文字以内にしてください。';
      case 'invalid-index':
        return '対象のキーワードが見つかりません。';
    }
  }
}
