import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-series-detail-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>シリーズ詳細</h1>
      <p>全巻リストと購読・購入操作ができます。</p>
    </main>
  `,
})
export class SeriesDetailPage {}
