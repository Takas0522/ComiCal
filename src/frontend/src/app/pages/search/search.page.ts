import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>検索</h1>
      <p>タイトル・著者・発売日・出版社で検索できます。</p>
    </main>
  `,
})
export class SearchPage {}
