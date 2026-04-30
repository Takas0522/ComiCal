import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { SearchBarComponent } from '../../molecules/search-bar/search-bar.component';
import { CardGridComponent } from '../../organisms/card-grid/card-grid.component';
import { Volume } from '../../molecules/volume-card/volume-card.component';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [SearchBarComponent, CardGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-search" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">検索</h1>
      <app-search-bar
        placeholder="タイトル・著者・出版社で検索..."
        [value]="query()"
        (search)="onSearch($event)"
        class="mb-6 block"
      />
      @if (query()) {
        <app-card-grid [volumes]="results" [loading]="isLoading()" />
      } @else {
        <p class="text-[--color-text-secondary] text-center py-16">
          キーワードを入力して検索してください。
        </p>
      }
    </div>
  `,
})
export class SearchPage {
  protected readonly query = signal('');
  protected readonly isLoading = signal(false);
  protected readonly results: Volume[] = [];

  onSearch(q: string) {
    this.query.set(q);
  }
}
