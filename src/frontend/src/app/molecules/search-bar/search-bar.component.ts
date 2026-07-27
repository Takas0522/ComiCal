import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form role="search" class="flex gap-2" (submit)="onSubmit($event)">
      <input
        data-testid="input-search"
        type="search"
        [placeholder]="placeholder()"
        [value]="value()"
        (input)="onInput($event)"
        class="flex-1 px-4 py-2 rounded-lg border border-[--color-border] bg-[--color-surface] text-[--color-text-primary] placeholder:text-[--color-text-secondary] focus:outline-2 focus:outline-[--color-primary]"
        aria-label="検索キーワード"
      />
      <button
        type="submit"
        data-testid="search-submit"
        class="px-4 py-2 bg-[--color-primary] text-white rounded-lg hover:bg-[--color-primary-hover] transition-colors"
        aria-label="検索"
      >
        検索
      </button>
    </form>
  `,
})
export class SearchBarComponent {
  readonly placeholder = input('検索...');
  readonly value = input('');
  readonly search = output<string>();

  private currentValue: string | undefined;

  onInput(event: Event) {
    this.currentValue = (event.target as HTMLInputElement).value;
  }

  onSubmit(event: Event) {
    event.preventDefault();
    this.search.emit(this.currentValue ?? this.value());
  }
}
