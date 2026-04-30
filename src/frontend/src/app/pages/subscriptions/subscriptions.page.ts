import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-subscriptions-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <main>
      <h1>購読一覧</h1>
      <p>購読中のシリーズ一覧を表示します。</p>
    </main>
  `,
})
export class SubscriptionsPage {}
