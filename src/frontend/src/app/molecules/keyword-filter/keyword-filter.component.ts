import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  MAX_KEYWORD_CHARACTERS,
  MAX_KEYWORDS,
  normalizeKeyword,
} from '../../features/upcoming-filter.store';

export interface KeywordUpdate {
  index: number;
  keyword: string;
}

@Component({
  selector: 'app-keyword-filter',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section aria-labelledby="keyword-filter-heading">
      <h2 id="keyword-filter-heading" class="sr-only" i18n>絞り込みキーワード</h2>

      <div class="flex gap-2">
        <label class="sr-only" for="keyword-filter-input" i18n>絞り込みキーワードを追加</label>
        <input
          id="keyword-filter-input"
          data-testid="keyword-filter-input"
          type="text"
          [ngModel]="newKeyword()"
          (ngModelChange)="newKeyword.set($event)"
          (keydown.enter)="addFromInput($event)"
          aria-describedby="keyword-filter-status"
          class="min-w-0 flex-1 rounded-lg border border-[--color-border] bg-[--color-surface] px-3 py-2 text-[--color-text-primary] focus:outline-2 focus:outline-[--color-primary]"
          placeholder="キーワードを追加"
          i18n-placeholder
        />
        <button
          type="button"
          data-testid="keyword-filter-add"
          (click)="addFromInput()"
          class="rounded-lg bg-[--color-primary] px-4 py-2 font-semibold text-white transition-colors hover:bg-[--color-primary-hover]"
          i18n
        >
          追加
        </button>
      </div>

      @if (keywords().length > 0) {
        <p class="mt-3 text-xs text-[--color-text-secondary]" i18n>
          タグを押すと編集、✕ で削除できます。
        </p>
        <ul
          class="mt-3 flex flex-wrap gap-2"
          aria-label="登録済み絞り込みキーワード"
          i18n-aria-label
        >
          @for (keyword of keywords(); track $index; let index = $index) {
            <li
              class="inline-flex items-center gap-1 rounded-full border transition-colors"
              [class.px-1]="editingIndex() === index"
              [class.py-1]="editingIndex() === index"
              [style]="
                editingIndex() === index
                  ? 'background: var(--color-surface); border-color: var(--color-primary)'
                  : 'background: var(--color-primary-light); border-color: transparent'
              "
            >
              @if (editingIndex() === index) {
                <label class="sr-only" [for]="'keyword-filter-edit-input-' + index" i18n>
                  {{ keyword }} を編集
                </label>
                <input
                  [id]="'keyword-filter-edit-input-' + index"
                  data-testid="keyword-filter-chip-edit-input"
                  type="text"
                  [ngModel]="editKeyword()"
                  (ngModelChange)="editKeyword.set($event)"
                  (keydown.enter)="confirmEdit($event)"
                  (keydown.escape)="cancelEdit()"
                  class="w-32 rounded-full border-0 bg-transparent px-2 py-0.5 text-sm text-[--color-text-primary] focus:outline-2 focus:outline-[--color-primary]"
                />
                <button
                  type="button"
                  data-testid="keyword-filter-chip-edit-confirm"
                  (click)="confirmEdit()"
                  [attr.aria-label]="keyword + ' の編集を確定'"
                  class="inline-flex h-7 w-7 items-center justify-center rounded-full bg-[var(--color-primary)] text-sm font-semibold text-white hover:bg-[var(--color-primary-hover)] focus:outline-2 focus:outline-offset-1 focus:outline-[--color-primary]"
                >
                  <span aria-hidden="true">✓</span>
                </button>
                <button
                  type="button"
                  data-testid="keyword-filter-chip-edit-cancel"
                  (click)="cancelEdit()"
                  [attr.aria-label]="keyword + ' の編集を取り消す'"
                  class="inline-flex h-7 w-7 items-center justify-center rounded-full text-sm text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-elevated)] focus:outline-2 focus:outline-[--color-primary]"
                >
                  <span aria-hidden="true">✕</span>
                </button>
              } @else {
                <button
                  type="button"
                  data-testid="keyword-filter-chip-edit"
                  (click)="startEdit(index)"
                  [attr.aria-label]="keyword + ' を編集'"
                  class="min-h-6 rounded-full py-1 pl-3 pr-1 text-sm font-medium text-[var(--color-primary-hover)] hover:underline focus:outline-2 focus:outline-[--color-primary]"
                >
                  {{ keyword }}
                </button>
                <button
                  type="button"
                  data-testid="keyword-filter-chip-remove"
                  (click)="remove.emit(index)"
                  [attr.aria-label]="keyword + ' を削除'"
                  class="mr-1 inline-flex h-7 w-7 items-center justify-center rounded-full text-sm text-[var(--color-primary-hover)] hover:bg-[var(--color-surface)] focus:outline-2 focus:outline-[--color-primary]"
                >
                  <span aria-hidden="true">✕</span>
                </button>
              }
            </li>
          }
        </ul>
      }

      <p
        id="keyword-filter-status"
        data-testid="keyword-filter-status"
        aria-live="polite"
        class="mt-2 text-sm text-[--color-text-secondary]"
      >
        {{ liveStatus() }}
      </p>
    </section>
  `,
})
export class KeywordFilterComponent {
  readonly keywords = input<readonly string[]>([]);
  readonly status = input<string | null>(null);
  readonly add = output<string>();
  readonly update = output<KeywordUpdate>();
  readonly remove = output<number>();

  protected readonly newKeyword = signal('');
  protected readonly editKeyword = signal('');
  protected readonly editingIndex = signal<number | null>(null);
  private readonly validationMessage = signal<string | null>(null);
  protected readonly liveStatus = computed(
    () =>
      this.validationMessage() ??
      this.status() ??
      `${this.keywords().length} 件の絞り込みキーワードが登録されています。`,
  );

  addFromInput(event?: Event) {
    event?.preventDefault();
    const keyword = normalizeKeyword(this.newKeyword());
    const validationMessage = this.validate(keyword);
    if (validationMessage) {
      this.validationMessage.set(validationMessage);
      return;
    }

    this.validationMessage.set(null);
    this.newKeyword.set('');
    this.add.emit(keyword);
  }

  startEdit(index: number) {
    this.editingIndex.set(index);
    this.editKeyword.set(this.keywords()[index] ?? '');
    this.validationMessage.set(null);
  }

  confirmEdit(event?: Event) {
    event?.preventDefault();
    const index = this.editingIndex();
    if (index === null) return;

    const keyword = normalizeKeyword(this.editKeyword());
    const validationMessage = this.validate(keyword, index);
    if (validationMessage) {
      this.validationMessage.set(validationMessage);
      return;
    }

    this.validationMessage.set(null);
    this.editingIndex.set(null);
    this.update.emit({ index, keyword });
  }

  cancelEdit() {
    this.editingIndex.set(null);
    this.editKeyword.set('');
    this.validationMessage.set(null);
  }

  private validate(keyword: string, editingIndex?: number): string | null {
    if (!keyword) return 'キーワードを入力してください。';

    if (
      this.keywords().some(
        (existing, index) => index !== editingIndex && normalizeKeyword(existing) === keyword,
      )
    ) {
      return '同じキーワードは登録できません。';
    }

    const next = this.keywords().map((existing, index) => {
      return index === editingIndex ? keyword : normalizeKeyword(existing);
    });
    if (editingIndex === undefined) next.push(keyword);
    if (next.length > MAX_KEYWORDS) {
      return `キーワードは${MAX_KEYWORDS}件まで登録できます。`;
    }
    if (next.reduce((total, item) => total + item.length, 0) > MAX_KEYWORD_CHARACTERS) {
      return 'キーワードの合計は512文字以内にしてください。';
    }

    return null;
  }
}
