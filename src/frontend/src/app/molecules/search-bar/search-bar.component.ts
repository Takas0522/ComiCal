import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  output,
  signal,
} from '@angular/core';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form
      role="search"
      class="flex gap-2 w-full"
      (submit)="onSubmit($event)"
      data-testid="search-bar"
    >
      <label class="sr-only" [for]="inputId()" i18n="@@searchBar.inputLabel">検索キーワード</label>
      <input
        [id]="inputId()"
        type="search"
        autocomplete="off"
        [placeholder]="placeholder()"
        [value]="value()"
        (input)="onInput($event)"
        class="flex-1 rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-brand-500)]"
        data-testid="search-bar-input"
        i18n-aria-label="@@searchBar.ariaLabel"
        aria-label="検索"
      />
      <button
        type="submit"
        class="rounded-md bg-[var(--color-brand-500)] px-4 py-2 text-sm font-medium text-white hover:bg-[var(--color-brand-700)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[var(--color-brand-500)]"
        data-testid="search-bar-submit"
        i18n="@@searchBar.submit"
      >検索</button>
    </form>
  `,
})
export class SearchBarComponent {
  readonly initialValue = input<string>('');
  readonly placeholder = input<string>('タイトル / 著者 / 出版社');
  readonly debounceMs = input<number>(300);
  readonly inputId = input<string>('search-bar-input');

  readonly searchTerm = output<string>();

  protected readonly value = signal('');
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Sync internal signal with the initial value input (one-way).
    effect(() => {
      this.value.set(this.initialValue());
    });
  }

  protected onInput(ev: Event): void {
    const next = (ev.target as HTMLInputElement).value;
    this.value.set(next);
    if (this.timer) clearTimeout(this.timer);
    const ms = this.debounceMs();
    if (ms <= 0) {
      this.searchTerm.emit(next);
      return;
    }
    this.timer = setTimeout(() => this.searchTerm.emit(next), ms);
  }

  protected onSubmit(ev: Event): void {
    ev.preventDefault();
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    this.searchTerm.emit(this.value());
  }
}
